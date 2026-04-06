const puppeteer = require("puppeteer");
const chokidar = require("chokidar");
const fs = require("fs");
const path = require("path");
const { exec } = require("child_process");

// Templates root is the parent directory (../), since preview.js lives in preview/
const TEMPLATES_ROOT = path.resolve(__dirname, "..");

const MARGIN_META_REGEX =
  /<meta\s+name="pdf-margin-(top|bottom|left|right)"\s+content="([^"]+)"/gi;

const PLACEHOLDER_REGEX = /\{\{(.*?)\}\}/g;

const LIST_TAG_REGEX = /<list:([^>]+)([^>]*)>([\s\S]*?)<\/list:\1>/g;

const EMPTY_TAG_REGEX = /<empty:([^>]+)([^>]*)>([\s\S]*?)<\/empty:\1>/g;

const HAS_TAG_REGEX = /<has:([^>]+)>([\s\S]*?)<\/has:\1>/g;

const LINK_STYLESHEET_REGEX =
  /<link\s+[^>]*rel\s*=\s*["']stylesheet["'][^>]*href\s*=\s*["']([^"']+)["'][^>]*\/?>|<link\s+[^>]*href\s*=\s*["']([^"']+)["'][^>]*rel\s*=\s*["']stylesheet["'][^>]*\/?>/gi;

function inlineExternalStyles(html, templateDir) {
  return html.replace(LINK_STYLESHEET_REGEX, (match, href1, href2) => {
    const href = href1 || href2;
    const cssPath = path.join(templateDir, href);
    if (fs.existsSync(cssPath)) {
      const css = fs.readFileSync(cssPath, "utf-8");
      return `<style>${css}</style>`;
    }
    console.warn(`CSS file not found: ${cssPath}`);
    return match;
  });
}

function loadPreviewData(templateDir) {
  const globalPath = path.join(__dirname, "preview-data.json");
  const localPath = path.join(templateDir, "preview-data.json");

  let globalData = {};
  let localData = {};

  if (fs.existsSync(globalPath)) {
    globalData = JSON.parse(fs.readFileSync(globalPath, "utf-8"));
  }

  if (fs.existsSync(localPath)) {
    localData = JSON.parse(fs.readFileSync(localPath, "utf-8"));
  }

  return deepMerge(globalData, localData);
}

function deepMerge(target, source) {
  const result = { ...target };
  for (const key of Object.keys(source)) {
    if (
      source[key] &&
      typeof source[key] === "object" &&
      !Array.isArray(source[key]) &&
      target[key] &&
      typeof target[key] === "object" &&
      !Array.isArray(target[key])
    ) {
      result[key] = deepMerge(target[key], source[key]);
    } else {
      result[key] = source[key];
    }
  }
  return result;
}

function getValueFromJson(keys, json) {
  if (!json || keys.length === 0) return null;
  let current = json;
  for (const key of keys) {
    if (current === null || typeof current !== "object" || Array.isArray(current))
      return null;
    const found = Object.entries(current).find(
      ([k]) => k.toLowerCase() === key.toLowerCase()
    );
    if (!found) return null;
    current = found[1];
  }
  return current;
}

function replaceListTags(html, values) {
  return html.replace(LIST_TAG_REGEX, (_, tagName, _attrs, innerHtml) => {
    const keys = tagName.trim().split(".");
    const arr = getValueFromJson(keys, values);
    if (!Array.isArray(arr)) return "";
    return arr
      .map((item) => {
        let processed = replaceListTags(innerHtml, item);
        processed = replacePlaceholders(processed, item);
        if (_attrs && _attrs.trim()) {
          return `<div${_attrs}>${processed}</div>`;
        }
        return processed;
      })
      .join("");
  });
}

function replaceEmptyTags(html, values) {
  return html.replace(EMPTY_TAG_REGEX, (_, tagName, _attrs, innerHtml) => {
    const keys = tagName.trim().split(".");
    const arr = getValueFromJson(keys, values);
    const isEmpty = !Array.isArray(arr) || arr.length === 0;
    if (!isEmpty) return "";
    let processed = replacePlaceholders(innerHtml, values);
    if (_attrs && _attrs.trim()) {
      return `<div${_attrs}>${processed}</div>`;
    }
    return processed;
  });
}

function replaceHasTags(html, values) {
  return html.replace(HAS_TAG_REGEX, (_, tagName, innerHtml) => {
    const keys = tagName.trim().split(".");
    const arr = getValueFromJson(keys, values);
    if (Array.isArray(arr) && arr.length > 0) {
      return replacePlaceholders(innerHtml, values);
    }
    return "";
  });
}

function replacePlaceholders(html, values) {
  return html.replace(PLACEHOLDER_REGEX, (match, keyPath) => {
    const keys = keyPath.trim().split(".");
    const value = getValueFromJson(keys, values);
    return value !== null && value !== undefined ? String(value) : match;
  });
}

function insertValues(html, values) {
  let result = replaceListTags(html, values);
  result = replaceEmptyTags(result, values);
  result = replaceHasTags(result, values);
  result = replacePlaceholders(result, values);
  return result;
}

function extractMargins(headerHtml, footerHtml) {
  const margins = { top: "20mm", bottom: "20mm", left: "20mm", right: "20mm" };
  for (const html of [headerHtml, footerHtml]) {
    let match;
    MARGIN_META_REGEX.lastIndex = 0;
    while ((match = MARGIN_META_REGEX.exec(html)) !== null) {
      margins[match[1].toLowerCase()] = match[2];
    }
  }
  return margins;
}

async function generatePdf(templateDir, outputPath) {
  const indexPath = path.join(templateDir, "index.html");
  const headerPath = path.join(templateDir, "header.html");
  const footerPath = path.join(templateDir, "footer.html");

  if (!fs.existsSync(indexPath)) {
    console.error(`index.html not found in ${templateDir}`);
    return;
  }

  const values = loadPreviewData(templateDir);

  if (values.Employee && values.Employee.Photo && !values.Employee.Photo.startsWith('data:')) {
    const photoPath = path.join(__dirname, values.Employee.Photo);
    if (fs.existsSync(photoPath)) {
      const ext = path.extname(photoPath).slice(1).toLowerCase();
      const mimeType = ext === 'jpg' ? 'jpeg' : ext;
      const base64 = fs.readFileSync(photoPath).toString('base64');
      values.Employee.Photo = `data:image/${mimeType};base64,${base64}`;
    }
  }

  let bodyHtml = fs.readFileSync(indexPath, "utf-8");
  bodyHtml = insertValues(bodyHtml, values);

  let headerHtml = fs.existsSync(headerPath)
    ? fs.readFileSync(headerPath, "utf-8")
    : "<div></div>";
  let footerHtml = fs.existsSync(footerPath)
    ? fs.readFileSync(footerPath, "utf-8")
    : "<div></div>";

  headerHtml = inlineExternalStyles(headerHtml, templateDir);
  footerHtml = inlineExternalStyles(footerHtml, templateDir);

  headerHtml = insertValues(headerHtml, values);
  footerHtml = insertValues(footerHtml, values);

  const margins = extractMargins(headerHtml, footerHtml);

  const browser = await puppeteer.launch({ headless: "new" });
  const page = await browser.newPage();

  const fileUrl = "file:///" + indexPath.replace(/\\/g, "/");
  await page.goto(fileUrl, { waitUntil: "networkidle0" });
  await page.setContent(bodyHtml, { waitUntil: "networkidle0" });

  await page.pdf({
    path: outputPath,
    format: "A4",
    printBackground: true,
    displayHeaderFooter: true,
    headerTemplate: headerHtml,
    footerTemplate: footerHtml,
    margin: margins,
  });

  await browser.close();
  console.log(`[${new Date().toLocaleTimeString()}] PDF generated: ${outputPath}`);
}


async function main() {
  const templateName = process.argv[2];
  if (!templateName) {
    console.log("Usage: node preview.js <TEMPLATE_NAME> [--watch]");
    console.log("Example: node preview.js DECLARACAO_DEPENDENTES --watch");
    console.log("\nAvailable templates:");
    const dirs = fs
      .readdirSync(TEMPLATES_ROOT)
      .filter((f) =>
        fs.statSync(path.join(TEMPLATES_ROOT, f)).isDirectory() &&
        fs.existsSync(path.join(TEMPLATES_ROOT, f, "index.html"))
      );
    dirs.forEach((d) => console.log(`  - ${d}`));
    process.exit(1);
  }

  const watchMode = process.argv.includes("--watch");
  const templateDir = path.join(TEMPLATES_ROOT, templateName);
  const outputPath = path.join(templateDir, "preview.pdf");

  if (!fs.existsSync(templateDir)) {
    console.error(`Template directory not found: ${templateDir}`);
    process.exit(1);
  }

  console.log(`Generating PDF for: ${templateName}`);
  await generatePdf(templateDir, outputPath);


  if (!watchMode) {
    console.log("Done. Use --watch to auto-regenerate on file changes.");
    return;
  }

  console.log(`Watching for changes in ${templateDir}...`);
  console.log("Press Ctrl+C to stop.\n");

  let debounceTimer = null;
  const globalDataPath = path.join(__dirname, "preview-data.json");
  const watcher = chokidar.watch([templateDir, globalDataPath], {
    ignored: [/(^|[\/\\])\./, /preview\.pdf$/, /node_modules/],
    persistent: true,
    ignoreInitial: true,
  });

  watcher.on("all", (event, filePath) => {
    const relative = path.relative(templateDir, filePath);
    console.log(`[${event}] ${relative}`);

    if (debounceTimer) clearTimeout(debounceTimer);
    debounceTimer = setTimeout(async () => {
      try {
        await generatePdf(templateDir, outputPath);
      } catch (err) {
        console.error("Error generating PDF:", err.message);
      }
    }, 300);
  });
}

main().catch(console.error);
