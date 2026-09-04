#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Gera o Relatório de Auditoria de Segurança (PDF) a partir de `achados.json`.

Pipeline (sem instalar nada — só biblioteca padrão do Python):
    achados.json  ->  relatorio-auditoria-seguranca.html  ->  Chrome headless  ->  PDF
                                                                    Ghostscript  ->  preview/pagina-N.png (opcional)

Uso:
    python gerar_relatorio.py                # gera HTML + PDF
    python gerar_relatorio.py --preview      # também rasteriza as páginas em preview/ (precisa do Ghostscript)
    python gerar_relatorio.py --so-html      # só o HTML (para inspecionar no navegador)

Variáveis de ambiente opcionais:
    CHROME_PATH   caminho do chrome.exe / msedge.exe
    GS_PATH       caminho do gswin64c.exe

Por que Chrome e não reportlab/matplotlib: na máquina onde o relatório foi gerado pela primeira vez
o proxy de rede adulterava os wheels baixados do PyPI (hash SHA-256 divergente), então instalar
pacote era inseguro. Chrome + Ghostscript já existiam localmente e não exigem download.
"""
from __future__ import annotations

import glob
import html
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

AQUI = Path(__file__).resolve().parent
DADOS = AQUI / "achados.json"
HTML_OUT = AQUI / "relatorio-auditoria-seguranca.html"
PDF_OUT = AQUI / "relatorio-auditoria-seguranca.pdf"
PREVIEW_DIR = AQUI / "preview"

# Paleta prescrita pela auditoria (severidade) + ponto forte
CORES = {
    "critica": "#B91C1C",
    "alta": "#EA580C",
    "media": "#D97706",
    "baixa": "#2563EB",
    "informativa": "#6B7280",
    "forte": "#059669",
}
ROTULOS = {
    "critica": "Crítica",
    "alta": "Alta",
    "media": "Média",
    "baixa": "Baixa",
    "informativa": "Informativa",
}
ORDEM_SEV = ["critica", "alta", "media", "baixa", "informativa"]


def e(s: object) -> str:
    """Escapa para HTML. Tudo que vem do JSON passa por aqui."""
    return html.escape("" if s is None else str(s), quote=True)


def md_lista(itens: list[str], marcador: str = "-") -> str:
    return "\n".join(f"{marcador} {i}" for i in itens)


# --------------------------------------------------------------------------------------
# Gráficos em SVG puro (rótulo direto em todo segmento/barra: a paleta de severidade é
# ordinal e alta↔média são tons vizinhos, então a cor nunca fica sozinha).
# --------------------------------------------------------------------------------------
def svg_rosca(contagem: dict[str, int]) -> str:
    import math

    total = sum(contagem.values())
    cx, cy, r, largura = 88, 95, 66, 24
    partes = [(s, contagem.get(s, 0)) for s in ORDEM_SEV if contagem.get(s, 0) > 0]
    out = ['<svg viewBox="0 0 300 190" role="img" aria-label="Achados por severidade">']
    if total == 0:
        out.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="#E5E7EB" stroke-width="{largura}"/>')
    ang = -math.pi / 2
    gap = 0.04  # ~2px de superfície entre segmentos
    for sev, n in partes:
        frac = n / total
        a0 = ang + (gap / 2 if len(partes) > 1 else 0)
        a1 = ang + frac * 2 * math.pi - (gap / 2 if len(partes) > 1 else 0)
        if len(partes) == 1:
            out.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="{CORES[sev]}" stroke-width="{largura}"/>')
        else:
            x0, y0 = cx + r * math.cos(a0), cy + r * math.sin(a0)
            x1, y1 = cx + r * math.cos(a1), cy + r * math.sin(a1)
            grande = 1 if (a1 - a0) > math.pi else 0
            out.append(
                f'<path d="M {x0:.2f} {y0:.2f} A {r} {r} 0 {grande} 1 {x1:.2f} {y1:.2f}" '
                f'fill="none" stroke="{CORES[sev]}" stroke-width="{largura}" stroke-linecap="butt"/>'
            )
        ang += frac * 2 * math.pi
    out.append(f'<text x="{cx}" y="{cy - 2}" text-anchor="middle" font-size="26" font-weight="700" fill="#111827">{total}</text>')
    out.append(f'<text x="{cx}" y="{cy + 16}" text-anchor="middle" font-size="10" fill="#6B7280">achados</text>')
    # legenda com rótulo direto (nome + contagem)
    y = 38
    for sev in ORDEM_SEV:
        n = contagem.get(sev, 0)
        out.append(f'<rect x="178" y="{y - 10}" width="12" height="12" rx="3" fill="{CORES[sev]}"/>')
        out.append(f'<text x="196" y="{y}" font-size="11" fill="#111827">{ROTULOS[sev]}</text>')
        out.append(f'<text x="292" y="{y}" font-size="11" font-weight="700" fill="#111827" text-anchor="end">{n}</text>')
        y += 26
    out.append("</svg>")
    return "\n".join(out)


def svg_barras(categorias: list[dict], achados: list[dict]) -> str:
    # barras horizontais, uma por categoria, empilhadas por severidade, total rotulado
    linhas = []
    for c in categorias:
        cont = {s: 0 for s in ORDEM_SEV}
        for a in achados:
            if a["categoria"] == c["id"]:
                cont[a["severidade"]] += 1
        linhas.append((c, cont, sum(cont.values())))
    maximo = max([t for _, _, t in linhas] + [1])
    W, esq, dir_ = 300, 92, 26
    alt_barra, passo, topo = 20, 34, 14
    H = topo + passo * len(linhas) + 10
    escala = (W - esq - dir_) / maximo
    out = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="Achados por categoria">']
    for i, (c, cont, total) in enumerate(linhas):
        y = topo + i * passo
        nome = f'{c["id"]}. {c.get("abrev", c["curto"])}'
        out.append(f'<text x="{esq - 8}" y="{y + 14}" font-size="11" fill="#111827" text-anchor="end">{e(nome)}</text>')
        x = esq
        if total == 0:
            out.append(f'<rect x="{esq}" y="{y}" width="3" height="{alt_barra}" fill="#E5E7EB"/>')
        for sev in ORDEM_SEV:
            n = cont[sev]
            if n == 0:
                continue
            w = n * escala
            out.append(f'<rect x="{x:.1f}" y="{y}" width="{max(w - 2, 1):.1f}" height="{alt_barra}" rx="3" fill="{CORES[sev]}"/>')
            if w >= 16:
                out.append(f'<text x="{x + w / 2 - 1:.1f}" y="{y + 14}" font-size="10.5" font-weight="700" fill="#FFFFFF" text-anchor="middle">{n}</text>')
            x += w
        rot = f"{total}" if total else "0 — sem achados"
        out.append(f'<text x="{x + 5:.1f}" y="{y + 14}" font-size="11" font-weight="700" fill="#111827">{rot}</text>')
    out.append("</svg>")
    return "\n".join(out)


# --------------------------------------------------------------------------------------
# HTML
# --------------------------------------------------------------------------------------
CSS = """
@page {
  size: A4;
  margin: 20mm 20mm 22mm 20mm;
  @top-center { content: "%(titulo_curto)s"; font-family: "Segoe UI", Arial, sans-serif; font-size: 8.5pt; color: #6B7280; }
  @bottom-right { content: "Página " counter(page) " de " counter(pages); font-family: "Segoe UI", Arial, sans-serif; font-size: 8.5pt; color: #6B7280; }
  @bottom-left { content: "Confidencial — uso interno"; font-family: "Segoe UI", Arial, sans-serif; font-size: 8.5pt; color: #9CA3AF; }
}
@page capa { margin: 0; @top-center { content: none; } @bottom-right { content: none; } @bottom-left { content: none; } }
* { box-sizing: border-box; min-width: 0; }
svg { display: block; width: 100%%; height: auto; }
img { max-width: 100%%; }
html, body { margin: 0; padding: 0; }
body { font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif; font-size: 10.5pt; line-height: 1.45; color: #111827; background: #fff; }
h1 { font-size: 20pt; margin: 0 0 10pt; color: #0F172A; letter-spacing: -0.01em; }
h2 { font-size: 15pt; margin: 18pt 0 8pt; color: #0F172A; border-bottom: 2px solid #E5E7EB; padding-bottom: 4pt; }
h3 { font-size: 12pt; margin: 14pt 0 6pt; color: #1F2937; }
p { margin: 0 0 8pt; }
code, pre { font-family: Consolas, "Cascadia Mono", "Courier New", monospace; }
code { background: #F3F4F6; padding: 1px 4px; border-radius: 3px; font-size: 9pt; }
pre { max-width: 100%%; background: #F8FAFC; border: 1px solid #E5E7EB; border-left: 3px solid #CBD5E1; padding: 7pt 9pt; font-size: 8.4pt; line-height: 1.38; white-space: pre-wrap; word-break: break-word; overflow-wrap: anywhere; margin: 6pt 0 8pt; border-radius: 4px; }
.quebra { page-break-before: always; }
.evitar-quebra { page-break-inside: avoid; }
.capa { page: capa; box-sizing: border-box; height: 297mm; width: 210mm; max-width: 210mm; padding: 28mm 22mm 20mm; display: flex; flex-direction: column; background: linear-gradient(160deg, #0F172A 0%%, #1E293B 55%%, #0B3B2E 100%%); color: #F8FAFC; page-break-after: always; }
.capa .marca { font-size: 10pt; letter-spacing: 0.22em; text-transform: uppercase; color: #A7F3D0; }
.capa h1 { color: #FFFFFF; font-size: 28pt; margin-top: 60mm; line-height: 1.2; }
.capa .sub { font-size: 12pt; color: #CBD5E1; margin-top: 6pt; }
.capa .meta { margin-top: auto; display: grid; grid-template-columns: 1fr 1fr; gap: 10pt 24pt; font-size: 10pt; }
.capa .meta div b { display: block; color: #A7F3D0; font-size: 8.5pt; text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 2pt; }
.capa .meta div span { color: #F1F5F9; }
.capa .escopo { margin-top: 18pt; font-size: 9.5pt; color: #E2E8F0; }
.capa .escopo ul { margin: 4pt 0 0 14pt; padding: 0; }
.capa .escopo li { margin: 1pt 0; }
.faixa { display: grid; grid-template-columns: repeat(5, 1fr); gap: 8pt; margin: 10pt 0 14pt; }
.tile { border: 1px solid #E5E7EB; border-radius: 6px; padding: 8pt 10pt; border-top: 4px solid; }
.tile .n { font-size: 22pt; font-weight: 700; line-height: 1; }
.tile .l { font-size: 9pt; color: #6B7280; margin-top: 3pt; }
.graficos { display: grid; grid-template-columns: 1fr 1fr; gap: 14pt; align-items: start; margin: 8pt 0 12pt; }
.figura { border: 1px solid #E5E7EB; border-radius: 6px; padding: 8pt; page-break-inside: avoid; overflow: hidden; }
.figura .cap { font-size: 9pt; color: #6B7280; margin-top: 4pt; }
.chip { display: inline-block; padding: 1px 7px; border-radius: 999px; color: #fff; font-size: 8.2pt; font-weight: 700; letter-spacing: 0.02em; white-space: nowrap; }
table { width: 100%%; table-layout: fixed; border-collapse: collapse; font-size: 9.2pt; margin: 6pt 0 10pt; }
th { text-align: left; background: #F1F5F9; color: #334155; font-weight: 700; padding: 5pt 6pt; border-bottom: 2px solid #CBD5E1; font-size: 8pt; text-transform: uppercase; letter-spacing: 0.03em; overflow-wrap: anywhere; }
td { padding: 5pt 6pt; border-bottom: 1px solid #E5E7EB; vertical-align: top; }
tr { page-break-inside: avoid; }
td.local { font-family: Consolas, "Cascadia Mono", monospace; font-size: 8.4pt; word-break: break-all; }
.metodo table td:first-child { font-weight: 700; white-space: nowrap; }
.forte { border-left: 4px solid %(forte)s; background: #ECFDF5; padding: 6pt 9pt; margin: 0 0 6pt; border-radius: 0 4px 4px 0; page-break-inside: avoid; }
.fraco { border-left: 4px solid %(critica)s; background: #FEF2F2; padding: 6pt 9pt; margin: 0 0 6pt; border-radius: 0 4px 4px 0; page-break-inside: avoid; }
.forte b, .fraco b { display: block; margin-bottom: 2pt; }
.forte .ev, .fraco .ev { font-family: Consolas, "Cascadia Mono", monospace; font-size: 8.2pt; color: #374151; word-break: break-all; }
.achado { border: 1px solid #E5E7EB; border-radius: 6px; padding: 8pt 10pt; margin: 0 0 9pt; page-break-inside: avoid; }
.achado .cab { display: flex; gap: 8pt; align-items: baseline; margin-bottom: 3pt; flex-wrap: wrap; }
.achado .cab .id { font-family: Consolas, monospace; font-size: 9pt; color: #6B7280; white-space: nowrap; }
.achado .cab b { font-size: 10.5pt; }
.achado .loc { font-family: Consolas, "Cascadia Mono", monospace; font-size: 8.4pt; color: #374151; margin: 2pt 0 4pt; word-break: break-all; }
.achado .cond { font-size: 9pt; color: #6B7280; }
.rec { display: grid; grid-template-columns: 44pt 1fr; gap: 8pt; margin: 0 0 8pt; page-break-inside: avoid; }
.rec .p { font-weight: 800; font-size: 12pt; color: #fff; border-radius: 6px; text-align: center; padding: 6pt 0; height: fit-content; }
.rec .p.P1 { background: %(critica)s; } .rec .p.P2 { background: %(alta)s; } .rec .p.P3 { background: %(media)s; } .rec .p.P4 { background: %(baixa)s; }
.issue { page-break-inside: auto; margin-bottom: 14pt; }
.issue .delim { font-family: Consolas, monospace; font-weight: 700; color: #0F766E; background: #F0FDFA; border: 1px dashed #5EEAD4; padding: 3pt 8pt; border-radius: 4px; display: inline-block; font-size: 9pt; }
.issue pre { border-left-color: #0F766E; page-break-inside: auto; }
.nota { font-size: 9pt; color: #6B7280; }
.toc ol { margin: 4pt 0 0 16pt; padding: 0; } .toc li { margin: 2pt 0; }
.sem { color: #6B7280; font-style: italic; }
"""


def chip(sev: str) -> str:
    return f'<span class="chip" style="background:{CORES[sev]}">{ROTULOS[sev]}</span>'


def render_issue_md(issue: dict, achados_por_id: dict) -> str:
    """Monta o Markdown completo da issue a partir dos campos estruturados."""
    linhas = [f"# {issue['titulo']}", ""]
    linhas.append("**Labels sugeridas:** " + ", ".join(f"`{l}`" for l in issue["labels"]))
    linhas.append("")
    linhas.append("## Problema")
    linhas.append(issue["problema"].strip())
    linhas.append("")
    linhas.append("## Evidência")
    for aid in issue["achados"]:
        a = achados_por_id[aid]
        linhas.append(f"### {aid} — {a['titulo']}")
        linhas.append(f"`{a['arquivo']}:{a['linha']}`")
        linhas.append("")
        linhas.append("```" + a.get("lang", ""))
        linhas.append(a["trecho"].rstrip())
        linhas.append("```")
        linhas.append(a["explicacao"].strip())
        linhas.append("")
    linhas.append("## Impacto")
    linhas.append(issue["impacto"].strip())
    linhas.append("")
    linhas.append("## Sugestão de correção")
    linhas.append(md_lista(issue["correcao"], "1."))
    linhas.append("")
    linhas.append("## Critérios de aceite")
    linhas.append(md_lista(issue["criterios"], "- [ ]"))
    return "\n".join(linhas)


def render(dados: dict) -> str:
    achados = dados["achados"]
    categorias = dados["categorias"]
    achados_por_id = {a["id"]: a for a in achados}
    contagem = {s: 0 for s in ORDEM_SEV}
    for a in achados:
        contagem[a["severidade"]] += 1
    total = len(achados)
    titulo = f"Relatório de Auditoria de Segurança — {dados['projeto']}"

    p = []
    # ---------------- capa ----------------
    p.append('<section class="capa">')
    p.append('<div class="marca">Auditoria de segurança de código</div>')
    p.append(f"<h1>{e(titulo)}</h1>")
    p.append(f'<div class="sub">{e(dados["subtitulo"])}</div>')
    p.append('<div class="escopo"><b>Escopo auditado</b><ul>')
    for s in dados["escopo"]:
        p.append(f"<li>{e(s)}</li>")
    p.append("</ul></div>")
    p.append('<div class="meta">')
    p.append(f'<div><b>Data</b><span>{e(dados["data"])}</span></div>')
    p.append(f'<div><b>Revisão auditada</b><span>{e(dados["revisao"])}</span></div>')
    p.append(f'<div><b>Método</b><span>{e(dados["metodo_resumo"])}</span></div>')
    p.append(f'<div><b>Achados</b><span>{total} ({", ".join(f"{contagem[s]} {ROTULOS[s].lower()}" for s in ORDEM_SEV if contagem[s])})</span></div>')
    p.append("</div></section>")

    # ---------------- nota metodológica ----------------
    p.append('<section class="metodo">')
    p.append("<h1>Nota metodológica</h1>")
    p.append(f"<p>{e(dados['metodologia_intro'])}</p>")
    p.append("<table><thead><tr><th style='width:26%'>Categoria pedida</th><th>Como foi mapeada para a stack detectada</th></tr></thead><tbody>")
    for c in categorias:
        p.append(f"<tr><td>{c['id']}. {e(c['nome'])}</td><td>{e(c['mapeamento'])}</td></tr>")
    p.append("</tbody></table>")
    p.append("<h3>Stack detectada</h3><table><tbody>")
    for k, v in dados["stack"]:
        p.append(f"<tr><td style='width:26%;font-weight:700'>{e(k)}</td><td>{e(v)}</td></tr>")
    p.append("</tbody></table>")
    p.append("<h3>Regras de classificação</h3><ul>")
    for r in dados["regras"]:
        p.append(f"<li>{e(r)}</li>")
    p.append("</ul>")
    p.append('<div class="toc"><h3>Sumário</h3><ol>')
    for t in ["Resumo executivo", "Pontos fortes e pontos fracos", "Achados detalhados por categoria", "Recomendações priorizadas", "Issues para o GitHub"]:
        p.append(f"<li>{t}</li>")
    p.append("</ol></div></section>")

    # ---------------- resumo executivo ----------------
    p.append('<section class="quebra">')
    p.append("<h1>1. Resumo executivo</h1>")
    p.append(f"<p>{e(dados['resumo'])}</p>")
    p.append('<div class="faixa">')
    for s in ORDEM_SEV:
        p.append(f'<div class="tile" style="border-top-color:{CORES[s]}"><div class="n">{contagem[s]}</div><div class="l">{ROTULOS[s]}</div></div>')
    p.append("</div>")
    p.append('<div class="graficos">')
    p.append(f'<div class="figura">{svg_rosca(contagem)}<div class="cap">Figura 1 — Achados por severidade (total {total}).</div></div>')
    p.append(f'<div class="figura">{svg_barras(categorias, achados)}<div class="cap">Figura 2 — Achados por categoria, empilhados por severidade.</div></div>')
    p.append("</div>")
    p.append("<h3>Leitura rápida</h3><ul>")
    for r in dados["leitura_rapida"]:
        p.append(f"<li>{e(r)}</li>")
    p.append("</ul></section>")

    # ---------------- fortes e fracos ----------------
    p.append('<section>')
    p.append("<h1 style='margin-top:18pt'>2. Pontos fortes e pontos fracos</h1>")
    p.append("<h2>2.1 Pontos fortes (verificados com evidência)</h2>")
    for f in dados["pontos_fortes"]:
        p.append(f'<div class="forte"><b>{e(f["titulo"])}</b>{e(f["descricao"])}<div class="ev">Evidência: {e(f["evidencia"])}</div></div>')
    p.append("<h2>2.2 Pontos fracos (riscos centrais)</h2>")
    for f in dados["pontos_fracos"]:
        p.append(f'<div class="fraco"><b>{e(f["titulo"])}</b>{e(f["descricao"])}<div class="ev">Achados: {e(", ".join(f["achados"]))}</div></div>')
    p.append("</section>")

    # ---------------- achados detalhados ----------------
    p.append('<section class="quebra">')
    p.append("<h1>3. Achados detalhados por categoria</h1>")
    p.append("<p class='nota'>Tabela-síntese seguida da ficha de cada achado (trecho literal, por que é explorável, condições de explorabilidade). Linhas referem-se à revisão auditada.</p>")
    for c in categorias:
        lista = [a for a in achados if a["categoria"] == c["id"]]
        p.append(f"<h2>3.{c['id']} {e(c['nome'])}</h2>")
        if c.get("nota"):
            p.append(f"<p>{e(c['nota'])}</p>")
        if not lista:
            p.append("<p class='sem'>Nenhum achado nesta categoria.</p>")
            continue
        p.append("<table><thead><tr><th style='width:15%'>Severidade</th><th style='width:37%'>Arquivo:linha</th><th>Descrição</th></tr></thead><tbody>")
        for a in sorted(lista, key=lambda x: ORDEM_SEV.index(x["severidade"])):
            p.append(f"<tr><td>{chip(a['severidade'])}</td><td class='local'>{e(a['arquivo'])}:{e(a['linha'])}</td><td><b>{e(a['id'])}</b> — {e(a['titulo'])}</td></tr>")
        p.append("</tbody></table>")
        for a in sorted(lista, key=lambda x: ORDEM_SEV.index(x["severidade"])):
            p.append('<div class="achado">')
            p.append(f'<div class="cab"><span class="id">{e(a["id"])}</span>{chip(a["severidade"])}<b>{e(a["titulo"])}</b></div>')
            p.append(f'<div class="loc">{e(a["arquivo"])}:{e(a["linha"])}</div>')
            p.append(f"<pre>{e(a['trecho'].rstrip())}</pre>")
            p.append(f"<p><b>Por que é explorável:</b> {e(a['explicacao'])}</p>")
            if a.get("condicoes"):
                p.append(f'<div class="cond"><b>Condições de explorabilidade:</b> {e(a["condicoes"])}</div>')
            p.append("</div>")
    p.append("</section>")

    # ---------------- recomendações ----------------
    p.append('<section class="quebra">')
    p.append("<h1>4. Recomendações priorizadas</h1>")
    for r in dados["recomendacoes"]:
        p.append(f'<div class="rec"><div class="p {r["prioridade"]}">{r["prioridade"]}</div><div><b>{e(r["titulo"])}</b><br>{e(r["descricao"])}<div class="nota">Achados: {e(", ".join(r["achados"])) if r["achados"] else "—"}</div></div></div>')
    p.append("</section>")

    # ---------------- issues ----------------
    p.append('<section class="quebra">')
    p.append("<h1>5. Issues para o GitHub</h1>")
    p.append("<p class='nota'>Cada bloco abaixo é o texto completo de uma issue em Markdown, pronto para copiar e colar. Os delimitadores <code>--- ISSUE n ---</code> / <code>--- FIM ISSUE n ---</code> não fazem parte da issue. Achados triviais do mesmo tema foram agrupados.</p>")
    for i, issue in enumerate(dados["issues"], start=1):
        md = render_issue_md(issue, achados_por_id)
        p.append('<div class="issue">')
        p.append(f'<div class="delim">--- ISSUE {i} ---</div>')
        p.append(f"<pre>{e(md)}</pre>")
        p.append(f'<div class="delim">--- FIM ISSUE {i} ---</div>')
        p.append("</div>")
    p.append("</section>")

    css = CSS % {"titulo_curto": titulo.replace('"', "'"), **CORES}
    return (
        "<!doctype html><html lang='pt-BR'><head><meta charset='utf-8'>"
        f"<title>{e(titulo)}</title><style>{css}</style></head><body>"
        + "\n".join(p)
        + "</body></html>"
    )


# --------------------------------------------------------------------------------------
# Ferramentas locais
# --------------------------------------------------------------------------------------
def achar_chrome() -> str | None:
    cands = [os.environ.get("CHROME_PATH")]
    for base in (os.environ.get("ProgramFiles"), os.environ.get("ProgramFiles(x86)"), os.environ.get("LocalAppData")):
        if base:
            cands += [
                os.path.join(base, "Google", "Chrome", "Application", "chrome.exe"),
                os.path.join(base, "Microsoft", "Edge", "Application", "msedge.exe"),
            ]
    cands += [shutil.which("google-chrome"), shutil.which("chromium"), shutil.which("chrome"), shutil.which("msedge")]
    for c in cands:
        if c and os.path.isfile(c):
            return c
    return None


def achar_gs() -> str | None:
    c = os.environ.get("GS_PATH")
    if c and os.path.isfile(c):
        return c
    for base in (os.environ.get("ProgramFiles"), os.environ.get("ProgramFiles(x86)")):
        if base:
            for hit in sorted(glob.glob(os.path.join(base, "gs", "*", "bin", "gswin64c.exe")), reverse=True):
                return hit
    return shutil.which("gswin64c") or shutil.which("gs")


def gerar_pdf(html_path: Path, pdf_path: Path) -> None:
    chrome = achar_chrome()
    if not chrome:
        sys.exit("Chrome/Edge não encontrado. Defina CHROME_PATH.")
    with tempfile.TemporaryDirectory(prefix="relatorio-chrome-") as perfil:
        cmd = [
            chrome,
            "--headless=new",
            "--disable-gpu",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-extensions",
            f"--user-data-dir={perfil}",
            "--no-pdf-header-footer",
            f"--print-to-pdf={pdf_path}",
            html_path.resolve().as_uri(),
        ]
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=180)
    if not pdf_path.exists() or pdf_path.stat().st_size < 1000:
        sys.exit(f"Falha ao gerar PDF.\n{r.stderr[-2000:]}")


def rasterizar(pdf_path: Path, destino: Path, dpi: int = 70) -> int:
    gs = achar_gs()
    if not gs:
        print("Ghostscript não encontrado; preview pulado. Defina GS_PATH.")
        return 0
    destino.mkdir(exist_ok=True)
    for f in destino.glob("pagina-*.png"):
        f.unlink()
    subprocess.run(
        [gs, "-q", "-dNOPAUSE", "-dBATCH", "-sDEVICE=png16m", f"-r{dpi}", f"-sOutputFile={destino / 'pagina-%02d.png'}", str(pdf_path)],
        check=True, capture_output=True, timeout=300,
    )
    return len(list(destino.glob("pagina-*.png")))


def contar_paginas(pdf_path: Path) -> int:
    dados = pdf_path.read_bytes()
    # contagem simples e suficiente para o PDF que o Chrome emite
    import re

    return len(re.findall(rb"/Type\s*/Page[^s]", dados))


def main(argv: list[str]) -> None:
    dados = json.loads(DADOS.read_text(encoding="utf-8"))
    HTML_OUT.write_text(render(dados), encoding="utf-8")
    print(f"HTML: {HTML_OUT}")
    if "--so-html" in argv:
        return
    gerar_pdf(HTML_OUT, PDF_OUT)
    print(f"PDF : {PDF_OUT} ({PDF_OUT.stat().st_size // 1024} KB, {contar_paginas(PDF_OUT)} páginas)")
    if "--preview" in argv:
        n = rasterizar(PDF_OUT, PREVIEW_DIR)
        print(f"Preview: {n} páginas em {PREVIEW_DIR}")


if __name__ == "__main__":
    main(sys.argv[1:])
