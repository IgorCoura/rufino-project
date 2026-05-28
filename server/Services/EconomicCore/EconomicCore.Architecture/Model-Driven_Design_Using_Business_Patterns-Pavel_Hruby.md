# Model-Driven Design Using Business Patterns

**Author:** Pavel Hruby

---


Model-Driven
Design Using
Business
Patterns
2) Springer


<!-- Page 1 -->


Model-Driven Design Using Business Patterns


<!-- Page 2 -->


Pavel Hruby

Model-Driven Design
Using Business Patterns
with Contributions by

Jesper Kiehn and Christian Vibe Scheller

g) Springer


<!-- Page 3 -->


Author

Pavel Hruby

Microsoft Development Center Copenhagen

Frydenlunds Allé 6

2950 Vedbzek, Denmark

phruby@acm.org

http://reatechnology.com

http://phruby.com

Library of Congress Control Number: 2006927040

ACM Computing Classification (1998): D.2.11, H.1, J.1

ISBN-10 3-540-30154-2 Springer Berlin Heidelberg New York

ISBN-13 978-3-540-30154-7 Springer Berlin Heidelberg New York

This work is subject to copyright. All rights are reserved, whether the whole or part of the material
is concerned, specifically the rights of translation, reprinting, reuse of illustrations, recitation, broadc
asting, reproduction on microfilm or in any other way, and storage in data banks. Duplication of
this publication or parts thereof is permitted only under the provisions of the German Copyright Law
of September 9, 1965, in its current version, and permission for use must always be obtained from
Springer. Violations are liable for prosecution under the German Copyright Law.

Springer is a part of Springer Science+Business Media

springer.com

© Springer-Verlag Berlin Heidelberg 2006

Printed in Germany

The use of general descriptive names, registered names, trademarks, etc. in this publication does not
imply, even in the absence of a specific statement, that such names are exempt from the relevant prot
ective laws and regulations and therefore free for general use.

Typeset by the author

Production: LE-TgX Jelonek, Schmidt & Véckler GbR, Leipzig

Cover design: KiinkelLopka Werbeagentur, Heidelberg

Printed on acid-free paper 45/3100/YL - 543210


<!-- Page 4 -->


To Mom and Dad


<!-- Page 5 -->


Preface

This book describes the REA (resources, events, agents) model, which is

going to revolutionize the way the software business applications are de-

veloped. REA specifies the fundamental laws of the business domain.

Knowing these laws radically enhances the application designers’ potential

to configure business solutions without omissions, and ensures consistency

of software applications from the business perspective:

e The application design based on REA is concise and easy to understand
both for the users of software applications, for consultants, and for app
lication developers. REA is a ubiquitous language ensuring unambiguo
us communication and understanding among all participants of the
software development process.

e Software applications based on REA contain more business knowledge
than applications developed merely from user requirements, and can
therefore advise and guide the users during development and configurat
ion, without restricting the end-users at runtime.

e The same modeling principles are used across all application areas in
the business domain; the sales, procurement, production, marketing,
human resources, finance, and other areas are described by a common
set of patterns.

e As REA software applications store the primary data about economic
resources, all reports and all accounting artifacts are always consistent,
because they are derived from the same data; for example, the data des
cribing the sale event is used in the warehouse management, payroll,
distribution, finance and other application areas, without transformat
ions or adjustments.

e The REA model provides for more complete, transparent, and up-tod
ate reporting for business decision makers than reporting based on the
accounting artifacts, which dominates in current business applications.
REA can be extended using a number of patterns that comprise func-

tionality necessary to build business applications that meet specific busi-

ness needs. REA alone specifies domain rules assuring that the application


<!-- Page 6 -->


VII. ——~ Preface

model is sound from the business perspective, and forms the backbone of

the application model. Knowing REA is useful but not sufficient to build a

business application; similarly, knowing only Maxwell’s laws is not suffi-

cient to build a radio transmitter and receiver. We describe the patterns that
extend REA in Part II of this book, Behavioral Patterns.

This book is primarily intended for
e Software architects, visionary managers, and anyone interested in un-

derstanding REA, and its strengths and limitations.

e Application developers designing business applications, and interested
in the consistency that accrues from the REA model.

e Framework developers designing business frameworks, looking for gene
ral business domain concepts and for the principles that apply to these
concepts.

e Students of university courses on enterprise information architectures.
We also include two implementation chapters illustrating how easy it is

to build an REA-based software application, and how to implement the

behavioral business patterns. A complete REA business application cont
aining the code samples from this book can be downloaded from
http://www.reatechnology.com.

Structure of This Book


# Part I, Structural Patterns, describes the REA model in detail. This part de-


scribes REA at the operational level (the level of actual business transac-

tions), and at the policy level, specifying which transactions could or
should happen. This part includes an implementation chapter that includes
executable code of an REA business application.


# Part II of this book, Behavioral Patterns, describes the patterns that ext

end REA with the functionality necessary to meet specific needs of the
users of a business application. This part also includes an implementation
chapter containing code for several behavioral patterns, as well as for the
infrastructure necessary to run the implementation.


# Part III of this book, Modeling Handbook, illustrates REA models in

which the use of REA is less straightforward, such as insurance, guarant
ees and taxes.

Appendix A describes REA as an ontology for business systems; we
recommend this to readers interested in the theoretical foundations of
REA.


<!-- Page 7 -->


Preface IX

Appendix B, Notes on Modeling, describes the relationships between
models, metamodels and the real world, describes how we use UML in this
book, illustrates REA models at various levels of granularity, and explains
modeling viewpoints in REA, the REA models in the trading partner view
and the independent view.

Appendix C, Patterns and Pattern Form, describes the pattern form used
for the patterns in this book.

The pattern map on the next page shows the patterns described in this
book. The purpose of the model at the operational level, i.e., the REA
EXCHANGE PROCESS, REA CONVERSION PROCESS and REA VALUE
CHAIN, is to describe what happened or was just happening in the user’s
business. The entities in these patterns determine the skeleton of the applic
ation model.

This skeleton can be extended by adding the entities described in the
patterns TYPE, GROUP, COMMITMENT, CONTRACT, SCHEDULE,
POLICY, LINKAGE, RESPONSIBILITY, and CUSTODY. These patterns
describe what could, should, or should not happen in the user’s business.
We call these REA models the policy level.

While all well-designed business applications more or less follow the
structure described in the structural patterns, the behaviors of different
business applications differ significantly, because specific requirements of
the users of the business applications differ. Therefore, the REA structural
skeleton at the operational and policy levels can be extended by patterns
that model specific behaviors of the business application. We refer to these
patterns as behavioral patterns. The patterns of general use are
IDENTIFICATION, DUE DATE, DESCRIPTION, NOTE, LOCATION,
CLASSIFICATION, NOTIFICATION, and VALUE; the patterns POSTING,
ACCOUNT, RECONCILIATION, and MATERIALIZED CLAIM have their
origins in financial applications. We will also describe a pattern for inventi
ng new behavioral patterns called IMVENTOR’S PARADOX.


<!-- Page 8 -->


X ~~ Preface
Pattern Map
Behavior Customizable Functionality
IDENTIFICATION DUE DATE DESCRIPTION NOTE
identity of entities deadlines external info internal info
CLASSIFICATION LOCATION NoTiFicaTion | | MATERIALIZED
grouping where events occur message invoices
POSTING ACCOUNT RECONCILIATION VALUE
keep history retrieve history match transactions units of measure
INVENTOR’S PARADOX
how to discover new behavioral patterns
REA Structure at Policy Level Extended Skeleton
What Could, Should or Should not Happen
POLICY COMMITMENT | | cornmitments | | commitments in
business rules future events . ‘
in trade production
TYPE GROUP LINKAGE RESPONSIBILITY CUSTODY
homogeneous heterogenous | | structure of structure of responsibility
collections collections resources agents for resources
REA Structure at Operational Level Fundamental Skeleton
What Has Happened
REA EXCHANGE REA CONVERSION
PROCESS PROCESS RE AOE CHAN
trade production P
start here


<!-- Page 9 -->


Preface XI

There are two natural starting points to using the patterns in this book. If
an application designer is able to identify the economic resources the users
want to plan, monitor, and control using the business application, then we
can start with the REA EXCHANGE PROCESS or REA CONVERSION
PROCESS patterns and integrate the processes into the REA VALUE
CHAIN. If application designers are not able to identify the economic res
ources, we can start with the REA VALUE CHAIN, and, using functional
decomposition of the processes, identify the level at which users want to
plan, monitor and control the resources, and then, by applying the REA
EXCHANGE PROCESS and REA CONVERSION PROCESS patterns,
identify the economic events and agents.

Internet Resources

The web site http://reatechnology.com contains executable code for the
REA application Joe’s Pizzeria, described in this book, as well as some
non-trivial REA models, extending those included in Part III, Modeling
Handbook. The mailing list at http://groups.yahoo.com/REATechnology is
a forum for researchers interested in theoretical foundations of REA, as
well as in solving specific modeling problems. The web site
http://hillside.net/ is dedicated to patterns in software development in gene
ral. UML diagrams in this book have been produced using Microsoft
Visio with a UML stencil available at http://phruby.com.
Acknowledgements

This book would not have been possible without the help of two disting
uished gentlemen, Jesper Kiehn and Christian Scheller.

First and foremost, Jesper Kiehn is one of few people who truly unders
tand what REA ontology is all about. Thanks to Jesper’s passion and ent
husiasm, countless long discussions and arguments, during which we
gradually began to understand REA, and the laws of the business domain.
Jesper’s many brilliant ideas helped me understand the full potential of
REA, what modeling business software really means, ontologies, knowle
dge management, and their consequences in sometimes surprisingly diff
erent application domains.

Christian Scheller is the inventor of the architecture that utilizes ort
hogonal separation of concerns in business domain. Christian was probab
ly the first person, back in 1999, who realized the potential of aspects for


<!-- Page 10 -->


XII ~—— Preface

modeling and implementing business logic components and not just nonf
unctional requirements; he is also a person who proved that business logic
could be fully and completely described at the model level, as opposed to
being described in a programming language. Christian wrote the implem
entation chapters in Part I and Part II of this book.

Many other people have helped and supported me along the way.

William E. McCarthy and Guido Geerts are the inventors of the REA
ontology. Thanks to both for their patience in explaining the REA to me,
for their visits to Copenhagen, for the many phone call meetings, and their
valuable insight.

Thanks to Ralph Johnson for superb guidance; and for suggesting that I
restructure Part I into its current form, with REA described as the exc
hange and conversion patterns.

Lars Hammer was the architect behind the Jamaica Project (1999-2002)
at Navision, which proved that the architecture described in this book
really does work, and has been a valuable in-house supporter.

This book can be seen as the result of NEXT, a shared project between
the Microsoft Development Center Copenhagen and the IT University Cop
enhagen. A special thanks to Kasper Osterbye for keeping the project
running, and Ph.D. students Mette Jaquet and Anders Hessellund whose
thesis attempted to specify the semantics of REA, and significantly imp
roved our understanding of this modeling framework.

I'd also like to express my thanks to the participants of the Software Arc
hitecture Group of University of Illinois at Urbana-Champaign, led by
Ralph Johnson, for discussing the manuscript for several weeks and maki
ng their discussions available to me, the participants of the writers’ works
hops at the conferences Viking PLoP 2002, 2004 and 2005 for their feedb
ack on the pattern style, and to Daniel May, Bob Hanmer, and Linda
Rising for shepherding the patterns.

For the generous assistance in the technical aspects of this book, I would
like to acknowledge the exceptional team at Springer, and especially my
editor Ralf Gerstner.

I would be remiss if I did not mention my family for creating a friendly
and enjoyable environment in which it was a pleasure to write this book
and to think about REA.

Finally, thanks to the reviewers of this book, especially Paul Johanness
on, Thomas Jensen, Krzysztof Czarnecki, Paul Adamczyk, Richard Kuo,
Geert Poels, and Bob Haugen for their comments on the manuscript in its
early stages, and to Allan Kelly, Janet Pehrson, and Keld Raaschou for
valuable feedback on specific parts of the book.


<!-- Page 11 -->


Table of Contents


# Part I Structural Patterns .............ccccccsssscsssscssssccssscccssccsssscccsscccssscsesscccees IL

1 Structural Patterns at Operational Level................sccssssssssesssseeee 3

### 1.1 What Is REA? ccc cccccccssscecsscccsssccessssesssscessseesssseessseeess


### 1.2 Joe’S PIZZA... ee eeeeccccesssscecessscccecesssecccssssccccesssseccessseccessseeeee D


#### 1.2.1 Salles Process ..........cccccccccsssscceesssseceessssecccessecceessteeceesseee O


#### 1.2.2 Purchase Process.............cccssccccsssseccessssccesssttecssstteeeees LL

1.2.3, Labor Acquisition Process ............ccccccsecceeceeteeeteeetreeee 12

#### 1.2.5 The Illustrated Models Are Examples of a Pattern ..... 13

1.3. REA Exchange Process Pattern.............:csscccscecereeeteeeteeetteeteeee LO

### 1.4 REA Exchange Process In Detail... ee ceesceeseeeeeeeeeeeeereee 19


#### 1.4.1 Economic ReSOUmCES.............ccssccccesssccceesssteceesssteceeenes LD


#### 1.4.2 Inflow and Outflow..........cc cece ccsssscceessteceessstteeeeees DL


#### 1.4.3 Ecomomic Events .............c:cccccsssecccesssccceessceseessseeeeesnee 20


#### 1.4.4 Exchange Duality... cece cesceseeeeeeeeceeecsteesteeeteees 20


#### 1.4.5 Economic Agent ..........ccccscccsceceteceseceeecsteeeteeeeeeeteeees OS


#### 1.4.6 Provide and RECEIVE .............cccssccceesssecceessseecessseeeeesses SD


### 1.5 How Joe’s Pizzeria Obtains PiZZa..............cccccccssssecceessteceeestees OO


#### 1.5.1 Producing Pizza... eeeeeeeceseceeececeeteeseeeeeeeetseetteees DO


1.5.3. The Pizza Production Process is an Example of
@ Pattern .........ceeecccssssecccesssccceessseccessseeceessseseesseeeeee 40

### 1.6 REA Conversion Process Pattern ..............c:ccccssscccesssseeceeseeee 4

1.7. REA Conversion Processes in Detail .................::ccccsssseeceeseeeee 49

#### 1.7.1 Economic ReSOULCES............ccccssccccesssccceesssseseesseeceeesss 4


#### 1.7.2 Produce, Use and Consume ...............cccccsssccceessteeeeesee 47


#### 1.7.3 Ecomomic Events .............c:ccccsssssceessseceesssseceesstseeeesseee OO.


#### 1.7.4 Conversion Duality 00.0... ccc ccceccceeeceerceeceeteeeteeesteeens 4


#### 1.7.5 Economic Agent ..........cccccscccssceeseceteceeeceteeeeeeeteesteeeee OD


#### 1.7.6 Provide and RECEIVE .............cccssscceesssecceessteeceessteeeeenss DO


### 1.8 Value Chain of Joe’s Pizzeria .0.........ccccccscsccecsssccceesseeeeeesseeees O2


### 1.9 REA Value Chain Pattern .0...........ccccceccsssscccesssseccessteeeessseeee OD


<!-- Page 12 -->


XIV Table of Contents

### 1.10 REA Value Chain in Detail ........ cece eesceeeceeeteceeeeeeeeteeeteeetnee TD


#### 1.10.1 Resource Value FIOWS ...........ccceeccessceeseeteeeeeeteetteesnee TD


#### 1.10.2 Economic Resources ..........:ccscceseesceeeeeeceeteeeteeteeteee DO


#### 1.10.3 Alternative Models of Business Processes..................81

2 Structural Patterns at Policy Level................csccsssssssssssssssscsessees BS

### 2.1 Group Pattern .0..... cc eecccecccececeeceeecseecseceeeeeeeneceaeesseeeeeeeeneeeaeees OF


### 2.2 Type Pattern .........ecceescecssccesccesseeseeeeeeessecenecseeceaeeeseeeeseeeneeeseees OO


### 2.3 Difference Between Types and Group5...........ccceeeceesceeseeeeeeee 92


### 2.4 Commitment Pattern... cecsecceecceeeceeseeeteeeteeeeeeeteeeneee DS


### 2.5 Contract Pattern... ceccssecetecetecetecsteceeeeeeeeeeseeeseetteetteeseee LOL


### 2.6 Schedule Pattern... ccscsseceseceseceteceeeeeeeeseeeeeeeeeeeseeeseee 108


### 2.7 Policy Pattern .0.......ccescecssccececeeceeceseeceeeeeeeeseeseeecsseeseesteeeseee LL2


### 2.8 Linkage Pattern 000.0... eecececseseceeceeeeeeeceeceeteeeteeeteeeteetteetteee LIQ


### 2.9 Responsibility Pattern ........... ce ccesccsscceseceeeeeeeceeseeeeceeteeeseeeeeee L22


### 2.10 Custody Pattern ..........ccceccssccssecesecsecceeeceeseeesceeeecteecseeeteestee L2S

3 An REA-Based Example Application.................sccscccscscssssssessres 129

### 3.1 Representing the Metamodel.............ceecesceeeeeeseeeeteeeteeeteeeeee 130


### 3.2 Component Model...........eceeeeccsescceeeceeceeeeeeeeeeeeeeteeeteetteeseee 130

3.3. The REA Model Component..............cccceesceetceeteeteetteetteeeeee 133

### 3.4 The Domain Model Component.............cccceceeceeteeteeetteeeeee 136


### 3.5 The Database .0...... cece ecccesscceseceeceecesecetecseeeseeeseeeesecesteesteeeseee LST


### 3.6 The Data Access Layer ........:.csccssccsseesteeeeeeeceeeeeeeetteetteeeeeee 138


### 3.7 Joe’S Web... eeeecccesccesceeseceececesecenecsaeceaceeeeeeeeeeseeeaeeeaeeeseeeseee LOO


### 3.8 The Fulfillment Page... ceeesceeeceteeeesteeteetteetteeeteee LAD


### 3.9 The OLAP Cube... cece cccceesecesecseceeeeceeeeeeaeeeaeeeeeeteeeteeeeeee L43


### 3.10 Conclusions .........cccsccescceseceseceseeeeeceeeceenceseeeteeetccetaceeaceteeesees L4G


# Part II Behavioral Patterne..............cccccccsscscsssscsssscscsccscsssssscssessscrseees LAD

4 Cross-Cutting Concer s............ccsscssssscsssscsscsscsscssscssesssscsersesseees LOL
4.1. Behavior May Not Be Localizable Into REA Entities ..........151

### 4.2 Framework-Based Approach..........:.ccccccssccsseceseeeteeeteeeteeseeeee LOD


### 4.3 There Is No Complete List of Behavioral Patterns ...............157

5 PatterMs...........ccccccccccccsccsccceccsccsccceccsccscccsccsccsesssesscsscsscssesssessssssesees LOD

### 5.1 Identification Pattern .............ccecescccesccesceeteeceeeceeeceeeeteeesteeee LOD


### 5.2 Classification Pattern... ccscccscccesceeseceteeeteeeteeetseeeeeeeeeess 166


### 5.3 Location Pattern 0.0.0... .ececeescessceesececceesceeseeeteeeteeeteeetteesteeeee LTA


### 5.4 Posting Pattern 20.0.0... cecccccssccesceeseeceeeceeceeecetecstecsteceseeeeeeeee 180


### 5.5 Account Pattern .........ceeesccseccssceeseeecceesceeeeeteeeteeeteeeteeeteeees 186


<!-- Page 13 -->


Table of Contents XV


### 5.6 Materialized Claim Pattern... ee eeseeseeseeteeteeeeeereeeee LOG


### 5.7 Reconciliation Pattern 0.0.0.0... ccccssccsceceteeeteeeteeeteeeeeeeeeeeteeeee 201


### 5.8 Due Date Pattern 0... ceecseceeceeeseceeeesceteesseeseeeeeteeseees 207


### 5.9 Description Pattern 0.0.0... ccccccscccscececeeceeteeeteesteeeeeeeteeeteeseee 213


### 5.10 Notification Pattern... cccceccescceseceeceeeeeeeeceeeeeteeeteeeteetee ZL


### 5.11 Note Pattern 20... eeeecseseeseeseeseeceeeeseeeeeeseeseeeeseseeeeseaeeneeeaees DOD


### 5.12 Value Pattern .2... eee ceessceseceecrceeeesseceeeecesecaseseesessaseaeeseees DLO


### 5.13 Inventor’s Paradox Pattern ............ccescecesceescceteeeteeetteettesteeeee DOL


6 An Aspect-Based Example Application..................cccssssssssesesres ZOD

### 6.1 Setting up the Application Model ...0.........ceecceeseeteeetteetteeens 23D


### 6.2 Creating the Aspect Code... ceecceccsscessteceteceteesteeteeetteetes 2OT


### 6.3 The Identification Aspect ........... cc eececceecceecceeteeeecetseeteesteeeers 238


### 6.4 The Due Date Aspect ........ cee ceecceseeseeteeceeceeceeceteesteeeteeeees 238


### 6.5 The Notification Aspect ..........cccescccsceceseceseeeteeeteeeteeeeeeeeeeeees 2AQ


### 6.6 The Description Aspect ...........cecceeccescceeceeceeeceeeeeteeeteeetteeeee 24]


### 6.7 Interchanging Events Between Aspects ............c:cccsceereeereeeee 241


### 6.8 Constructing the User Interface... eee eeeeseesteeeeeeeeeeeneeene 242


### 6.9 A Model-Based Framework ..........:ccccescceseeeseceeceeeeeeeteestee 245


### 6.10 Storage ...... cee eeceeccesceeeccsseeseeeeeeeeeeceeaeeeaeeseeeeeseeeeeeeteesteestes ZO L


### 6.11 Storing Aspect Data in Separate Tables .......... ccc eeseeseesteeens 20D


Part TIT Modeling Handbook...................ccccccsssssssssssssssssscssseccsccsscessssses LOD
7 Elementary Exchange Processes ...............csccscscscscssssscsscsssessseees OL
TL Cash Sale... cee essececseseeseeeeeeeeeeetseeeeeseeessaseeeesesesecseeenees 2O2


### 7.2 Product Return... es eeeescssccseeeeeseceeseseeeeeseceeeeeeeseseseaeeseees DOD


### 7.3 Loan and Rent (Individually Identifiable Resources) ...........268


### 7.4 Financial Loan (Nonindividually Identifiable Resources) ....271


8 Elementary Conversion Processes ...........ssssscssssssssscssssscssessssees 275

### 8.1 Creating a New Product .0....... eee ceeccesccesceeeeceeeeeeeeeeesteeteeeeeeee 270


### 8.2 Chain of Conversion Processes ...........c:ccssceseceeeeeseeeteeeeeeeeree 200


### 8.3 Modifying a Product... eeceesccesccssceeeeceeeceesceeeecneeceaeeeeestes 2OS


### 8.4 Creating and Consuming Services ...........cccesceeceeeeeeeeeereeeree 207


9 Value Chains with Exchange and Conversion Processes..........291

### 9.1 Sale and Shipment .......... cece eeecsccescccecceeceeeeeseeeeeeeeeeeeeeeeeeeee DOD


### 9.2 Resources Consumed During the Sales Process..................:.294


### 9.3 People Management ..............ccccescccesccesceeteeeteeeeeeceeeeeeeeteeetee 2OT


### 9.4 Education nn... eee eseesecesecseeeeeeectseeeeeeseeseeeeseaeeseteaeeseteeees SOO


D5 Taxes oe escscsssesceseesssesececeesceeeeeseeseeeeseseeseeeeeeseeseeesesetesesseees SOS


<!-- Page 14 -->


XVI ___ Table of Contents

### 9.6 Marketing and Advertising............:ccccsscssceceteeetceeteeeteeetteeeee SLO


### 9.7 Wate... esceesesscseesecesecesceseesesesceseeeaeeseeeaeeseseeeaeesesessereesesesees SIS


### 9.8 Purchasing and Selling Services ............cceseeseeeeteeeteeeteeeereees S16


### 9.9 Transient ReSOULCES 0.0.0... eeseeeeesececeeececeeesesceeesesseeeeeeesees SLD

10 = Processes With Contract ...........scccsssscsssesssssesscsscsscsessessessessessers SLD

### 10.1 Purchase Order... ec eeceseesceseseseceeeseeeeeteesseeeeeasesesseseneees 24


### 10.2 Labor Acquisition ............cccsscessceseceseceseeeeseceeeeesecsseeseesteessees BDO


### 10.3 Guarantee... eee ceecscesesesecseesseeseeeecsseeeseseeeeseseseeseseaeeeeees SOO.


### 10.4 Insurance... eee sseeeeseeseeeeecsceeeceseceeeeseesessaeeneeseseseeeeesseeseees SOO


### 10.5 Penalty for Violated Commitment...............cceeeeeeteeeteeteeetee B30


### 10.6 Schedule... eseseseesseeseeeeceececsescesessaeeseeseessecseeseetseeneese SOO


### 10.7 Transport......ccceecccesccesceeeecececeseceaeceeceescestecseeeseeceeseeteeeteeseee S4L

APPeNdCe............ccsccccscscsccscscscscccssccvecccsccesccsscssscsssccssscssccssscssscsssccssesses SHO
A. REA Ontology ............sccscssccsscssssccssseccescescessessessesscsscssssessessessessesseees SAT
B. Notes on Modeling................cccccscscscssscssssscsscssscsccssccssccssccssscssscsssssees SOL
B.1 There Is No Top-Level Business Process ............:eseseeeeeees SOL
B.2 Premature Sequential Ordering Is Not Advisable..................351
B.3 Bottom-Up Approach for Designing the System, and
Top-Down Approach for Explaining It Are Advisable.........352
B.4 Trading Partner View and Independent View..................2:+.393
B.5 Levels of Gramularity .0........ceeecceecceeecesceeeecececteeceaeesteesteeees SOA
B.6 Models, Metamodels and UML ........ ec ceeecssseseceeeeseeeeeeeeesees SOD
C. Patterns and Pattern Form ............ccccsssssscsccescsscsssessesscsscssssessesves SOD
References............cccsccsccscesscsscssccscssccssceccescessessessessessesscssesessessessessessessesee SOL
Index ..........csscscsccccsccncscsccsccscccsccsccseccsccseccscssccssssncsscsssscncssecssessessscssessecsses SOD


<!-- Page 15 -->


Part | Structural Patterns
This part describes REA in detail, i.e. the patterns for a skeleton and fund
amental structure of entities in a business software application. By using
the patterns in Part I, an application designer should end up with a struct
ure that is consistent, and without omissions from the business perspect
ive.
This part consists of two sections: structural patterns at operational
level, and structural patterns at policy level.
Behavior Customizable Functionality
REA Structure at Policy Level Extended Skeleton
What Could, Should or Should not Happen
in trade production
TYPE GROUP LINKAGE || RESPONSIBILITY | | CUSTODY
homogeneous || heterogenous | | structure of structure of responsibility
collections collections resources agents for resources
REA Structure at Operational Level Fundamental Skeleton
What Has Happened
REA EXCHANGE REA CONVERSION REA VALUE
PROCESS PROCESS CHAIN
trade production business processes


<!-- Page 16 -->


1 Structural Patterns at Operational Level
The first chapter in this section describes What is REA, and the chapter
Joe’s Pizzeria illustrates the fundamental interactions between the enterp
rise and its trading partners, that are examples of the REA EXCHANGE
PROCES pattern. The chapter Notes on Exchange Processes describes the
exchanges in more detail. The chapter How Joe’s Pizzeria Obtains Pizza
describes how the enterprise creates its products or services; these proce
sses are examples of the REA CONVERSION PROCESS PATTERN. The
section Notes on Exchange Processes describes the conversions in more
detail. The pattern REA VALUE CHAIN explains how to combine the REA
business processes into the chain of business processes of the enterprise.

Behavior Customizable Functionality

REA Structure at Policy Level Extended Skeleton

What Could, Should or Should not Happen

REA Structure at Operational Level Fundamental Skeleton

What Has Happened

REA EXCHANGE REA CONVERSION REA VALUE
PROCESS PROCESS CHAIN
trade production business processes


<!-- Page 17 -->


4 1 Structural Patterns at Operational Level


### 1.1 What Is REA?


There are several concepts that are present in almost all business software
applications. Understanding these concepts makes it much easier to design
business applications, to ensure that they do not violate the domain rules,
and to adapt the applications to changing requirements without the need to
change the overall architecture.

These concepts are known as REA (Resources, Events, Agents). Fig. 1
illustrates the most fundamental REA concepts, which are economic res
ource, economic agent, economic event, commitment, and contract.

rt
~
clause
provide
Commitment
reservation receive
reciprocity
fulfillment
provide
~ somccmt |) et
cconomic Event
Resource Agent
receive
| | increment a decrement | |
linkage duality responsiblity
Fig. 1. Fundamental REA concepts

Economic Resource is a thing that is scarce, and has utility for economic
agents, and is something users of business applications want to plan, monit
or, and control. Examples of economic resources are products and serv
ices, money, raw materials, labor, tools, and services the enterprise uses.

Economic Agent is an individual or organization capable of having cont
rol over economic resources, and transferring or receiving the control to
or from other individuals or organizations. Examples of economic agents
are customers, vendors, employees, and enterprises. The enterprise is an
economic agent from whose perspective we create the REA model.

Economic Event represents either an increment or a decrement in the
value of economic resources that are under the control of the enterprise.
Some economic events occur instantaneously, such as sales of goods; some


<!-- Page 18 -->


### 1.1 WhatIsREA? 5

occur over time, such as rentals, labor acquisition, and provision and use of
services.

Commitment is a promise or obligation of economic agents to perform
an economic event in the future. For example, line items on a sales order
represent commitments to sell goods.

Contract is a collection of increment and decrement commitments and
terms. Under the conditions specified by the terms, a contract can create
additional commitments. Thus, the contract can specify what should happ
en if the commitments are not fulfilled. For example, a sales order is a
contract containing commitments to sell goods and to receive payments.
The terms of the sales order contract can specify penalties (additional
commitments) if the goods or payments have not been received as promi
sed.

REA also specifies the domain rules assuring soundness and consistency
of business software applications from the business perspective. There are
several other approaches attempting to describe the fundamental modeling
entities, such as archetypes (Coad, Lefebvre, DeLuca. 1999) and pleom
orphs (Arlow, Neustadt 2003), for the business domain, and many busin
ess patterns on more detailed levels; our favorite books include (Fowler
1996), (Hay 1996), (Silverstone 1997), (Marshall 2000), and (Evans 2003).
The patterns and modeling entities described in these books can be exp
ressed in terms of the REA concepts. These patterns are more specific, as
they focus on certain subdomains within the business domain. They prov
ide for further concepts, but do not change the concepts defined in the
REA. Therefore, REA defines a ubiquitous language for business systems.

The fundamental idea of the REA model is

If an enterprise wants to increase the total value of resources under

its control, it usually has to decrease the value of some of its re-

sources.

An enterprise can increase or decrease the value of its resources either
by exchanges or by conversions.

e Exchange is a process in which an enterprise receives economic res
ources from other economic agents, and it gives resources to other econ
omic agents in return.

e Conversion is a process in which an enterprise uses or consumes res
ources in order to produce new or modify existing resources.

The data associated with exchanges and conversions are the primary
business data about the enterprise in REA software applications. Account-


<!-- Page 19 -->


6 1 Structural Patterns at Operational Level

ing artifacts such as debit, credit, journals, ledgers, receivables, and acc
ount balances are derived from the data describing the exchanges and
conversions. For example, the quantity on hand for an inventory item can
be calculated from the difference between the purchase and sale events, or
between the production and consumption events, for that inventory item.

For comparison, in most current business software applications, whose
paradigms are derived from double entry accounting, it is the opposite —
they focus on the accounting artifacts, and economic data is derived from
them. This, in some sense, puts the consequences before the cause and
makes the models more complicated.

The fact that REA operates on primary and raw economic data explains
why it offers a wider, more precise, and more up-to-date range of reports
than models based on the traditional double entry accounting system that
operates on derived accounting data.

REA was originally proposed as a generalized accounting model. It was
first published by William E. McCarthy of Michigan State University
(McCarthy 1982). McCarthy in his doctoral thesis at the University of
Massachusetts analyzed a large number of accounting transactions, and
identified their common features and formulated a general model describi
ng and explaining the accounting transactions. Since then, the original
REA model has been extended by McCarthy and Guido Geerts to a
framework for enterprise information architectures and ontology for busin
ess systems (Geerts, McCarthy 2000a, 2002). REA became the foundat
ion for several electronic interchange standards, such as ebXML and
Open-edi (an ISO standard), which influenced the extensions of the origin
al REA model into commitments and contracts.

Last but not least, an increasing number of business analysts have found
that the models they develop become better when they have REA in mind.


<!-- Page 20 -->


### 1.2 Joe’s Pizzeria 7


### 1.2 Joe’s Pizzeria

D Laces |
“a or ie ws ae vis oes
ee pe “ll
We will create an REA model for Joe’s Pizzeria
Joe makes revenue by selling pizza to his customers. Joe’s Pizzeria has
employees whose task is to sell pizzas, as well as to produce pizzas from
raw materials such as dough, tomatoes, cheese, pepperoni and other topp
ings. There are also other things necessary to produce pizza, such as the
oven where the pizza is baked, electricity consumed to heat the oven, vario
us kitchen equipment and many other things. Joe is interested in tracking
information about some of them; in REA, the things that Joe is interested
in planning, monitoring and controlling are called economic resources. Joe
has decided that the economic resources that will be included in the busin
ess software application are the Pizza, Cash, Labor of the employees, and
Raw Materials and Ingredients for producing pizza.
¢ Customer
Cash Pizza
Raw Materials
and Ingredients Labor
es —______.
¢ Joe’s Pizzeria t
dt >
Vendor Cash Cash Employee
Fig. 2. Trading partners of Joe’s Pizzeria
Trading partners of Joe’s Pizzeria are customers, vendors and employe
es. They are capable of controlling economic resources; therefore, in the


<!-- Page 21 -->


8 1 Structural Patterns at Operational Level
REA application model the Customer, Vendor, Employee, and Joe’s Pizzer
ia are economic agents, see Fig. 2.
Joe’s Pizzeria
(The enterprise) Pizza «exchange process»
Sales
«conversion process»
Raw Material
Labor
«exchange process») _ «exchange process»
Labor Acquisition Purchase

Fig. 3. Trading processes of Joe’s Pizzeria

The main trading processes of Joe’s Pizzeria, see Fig. 3, are selling
pizza to the customers (the Sales process), purchasing raw materials from
the vendors (the Purchase process), and purchasing labor from the emp
loyees (the Labor Acquisition process). We will construct the REA
model for each process.

#### 1.2.1 Sales Process

The process of selling pizza to the customers is essentially an exchange of
pizza for cash; Joe’s Pizzeria gives Pizza to the customer, and receives
Cash in return. For Joe’s Pizzeria, the Sales process represents an outflow
of Pizza and an inflow of Cash, see Fig. 4.

«exchange process»

Fig. 4. Selling pizza is an exchange of pizza for cash

The REA model for the process of selling pizza is illustrated in Fig. 5.
Joe’s Pizzeria and the Customer are economic agents, and the Pizza and
Cash are economic resources. One economic event is the transfer of own-


<!-- Page 22 -->


### 1.2 Joe’s Pizzeria 9

ership of the Pizza from Joe’s Pizzeria to the Customer (we call this event
Sale); in this transaction Joe’s Pizzeria provides Pizza, and Customer rec
eives it. Another economic event is the transfer of ownership of Cash
from the Customer to Joe’s Pizzeria (we call it Cash Receipt); in this
transaction the Customer provides Cash, and Joe’s Pizzeria receives it.

For Joe’s Pizzeria, the Sale event (the transfer of ownership of the Pizza
to the Customer) is a decrement event, because it decreases the value of
the resources under the control of Joe’s Pizzeria. The Cash Receipt is an
increment event, because it increases the value of the resources under the
control of Joe’s Pizzeria. The terms decrement and increment are relative
to the model viewpoint; they depend on the economic agent which is in the
focus of the model. If we modeled the same process from the perspective
of the Customer, the transfer of pizza would be an increment (would be
called Purchase) and the transfer of cash would be a decrement event
(would be called Payment or Cash Disbursement).

Joe’s Pizzeria Customer
srecelver “provide — «provide» «receive»
Cash Receipt Sale
«inflow» «outflow»
«resource» «resource»
Fig. 5. The REA model for Joe’s Pizzeria sales process

The REA model of the sales process in Fig. 5 focuses on the core econ
omic phenomena, and therefore it covers many special cases. For examp
le, most customers pay when they purchase pizza, but some customers
may receive an invoice, and pay for all their purchases in a certain period
at once. If the case of Internet sales, customers must provide their credit
card information before the pizza is delivered, and Joe’s Pizzeria receives
money from the customer’s bank later. When the sale occurs in the restaur
ant, the customers pay after they get pizza, either using cash or a credit
card.


<!-- Page 23 -->


10 1 Structural Patterns at Operational Level

All these cases are covered by the model in Fig. 5; this is very useful if
we would like to create a robust skeleton of a software application.

Customers may order pizza over the Internet. In this case, a software
business application creates an electronic Sales Order, which specifies a
commitment of Joe’s Pizzeria to sell a specified Pizza to the Customer,
and a commitment of a Customer to pay for the Pizza a specified amount
of Cash.

The Sales Order, see Fig. 6, is an example of a contract between the
economic agents Joe’s Pizzeria and the Customer. The Sales Line and the
Payment Line are not economic events; they are commitments to perform
the economic events in well-defined future. The Sales Line is a commitm
ent to perform the event Sale, and the Payment Line is a commitment to
perform the event Cash Receipt in the future.

«party» «party»

«economic agent» «contract» «economic agent»

Joe’s Pizzeria Sales Order Customer

«provide» aclauses «clauses «receive»

i «increment «decrement
“rece commitment» «exchan commitment» «provide»
— i ge Sales Line —
Payment Line reciprocity» a
«fulfillment» «fulfillment»
«exchange
«increment event» duality» «decrement event»
Cash Receipt Sale
«reservation» «inflow» «outflow» «reservation»
«resource» «resource»
Cash Pizza
Fig. 6. The REA model for the sales process with sales order

The Sales Order often contains terms specifying what should happen if
the commitments are not fulfilled, such as when the payment arrives late,
or the customer is not satisfied with the pizza. The fact that a contract can
be represented as a computer model is important for automatic tracking of
the state of the contract at runtime, and also for computer-assisted evaluat
ion of complicated financial contracts.


<!-- Page 24 -->


### 1.2 Joe’s Pizzeria 11


#### 1.2.2 Purchase Process

When Joe’s Pizzeria purchases tomatoes, cheese, pepperoni, flour and
other raw materials, it essentially exchanges the raw material for cash.
Vendor gives Raw Material to Joe’s Pizzeria, which gives it Cash in ret
urn. For Joe’s Pizzeria, the Purchase process represents an outflow of
Cash and an inflow of Raw Material, see Fig. 7.
«exchange process» F
Cash Raw Material
Fig. 7. Purchasing raw material is an exchange of raw material for cash
The REA model for the process of purchasing raw material is illustrated
in Fig. 8.
«econo! mic agent» «economic agent»
Joe’s Pizzeria Vendor
F «provide»
«receive» — «provide» «receive»
«increment event» «exchange» “cecroment event»
asl
Purchase Disbursement
«inflow» «outflow»
«resource» «resource»
Raw Material Cash
Fig. 8. The REA model for the purchase process
The Vendor and Joe’s Pizzeria are economic agents, the Raw Material
and Cash are economic resources. The transfer of ownership of the Raw
Material from the Vendor to Joe’s Pizzeria is an increment economic
event (we call it Purchase), and the transfer of ownership of Cash from
Joe’s Pizzeria to the Vendor (we call it Cash Disbursement) is a decrement
economic event; the increment and decrement are from Joe’s Pizzeria pers
pective.
Similarly as for the REA model for sales, the REA model for purchases
covers many special cases. Some raw materials can be paid by check or
bank transfer; some can be made in different currencies. There can be sev-


<!-- Page 25 -->


12 1 Structural Patterns at Operational Level
eral purchases paid using a single payment, and a single purchase can be
paid in several installments. The model tracks the information about which
purchases correspond to which cash disbursements, but abstracts from
technical details and does not specify the order of these transactions.
Again, this is useful if the skeleton of a software application is based on
this model, because it does not have to be changed if some technical asp
ects of the purchase process change.

#### 1.2.3 Labor Acquisition Process

Joe’s Pizzeria employees provide their work (they produce and sell pizzas
during specified periods of time) and receive their salary in return. Labor
acquisition is essentially an exchange of Labor (the worked hours) for
Cash. Employee sells his labor to Joe’s Pizzeria, which gives him Cash in
return. For Joe’s Pizzeria, the Labor Acquisition process represents an outf
low of Cash and an inflow of Labor, see Fig. 9.
«exchange process»
Cash Labor
Fig. 9. Labor acquisition is an exchange of worked hours for cash
O\oe's me agent» «economic agent»
ioe Ss Pizzeria Employee
. «provide».
«receive» — «provide» «receive»
«increment event» «exchange» «decrement event»
Labor Acquisition Disbursement
«inflow» «outfiows
«resource» «resource»
Labor Cash

Fig. 10. The REA model for the labor acquisition process

The REA model for the labor acquisition process is illustrated in
Fig. 10. The Employee and Joe’s Pizzeria are economic agents; the Emp
loyee provides Labor and receives Cash, and Joe’s Pizzeria provides


<!-- Page 26 -->


### 1.2 Joe’s Pizzeria 13

Cash and receives Labor. Labor (the worked hours) and Cash are econ
omic resources. The Labor Acquisition is an economic event that occurs
over periods of time (during the employee’s working hours), while Cash
Disbursement is an instantaneous event that occurs once a week or month
when the Employee receives his paycheck.

The REA model in Fig. 10 can be applied to many forms of acquiring
labor; it can be applied for full employment, temporary work, consulting,
as well as for work acquired according to various other forms of contracts.

#### 1.2.4 Summary

The REA model focuses on the core economic phenomena and abstracts
from technical and implementation details. This has several advantages.

Firstly, the REA model abstracts from the technical aspects of the transf
er of the resources. Cash can be transferred as bills and coins, as a check
or as a credit card transaction. Customers can pick pizza themselves, or
pizza can be delivered to their address. For all these cases we can apply the
same REA model, which does not have to be modified even if the technic
al infrastructure supporting the business changes.

Secondly, the REA model abstracts from the order in which the econ
omic events occur. Usually, pizza is paid at about the same time as it is
given to the customer, but sometimes it is paid for beforehand, and somet
imes it can be paid by credit card and there is a significant delay between
the sale of pizza and the transfer of cash. If the business process was specif
ied as a scenario consisting of a sequence of events, the business applicat
ion would support only the scenarios identified at design time. The REA
model allows the business application to flexibly record everything that act
ually happened. The actual order of events emerges at runtime, rather than
being specified at design time.

Thirdly, for each REA model apply certain rules: each increment must
be related to a decrement, each economic event must have a provider and
recipient agent, and each resource must be related to both increment and
decrement. Therefore, application designers can ask relevant questions
leading to the discovery of missing information in the user requirements,
and can construct the model even if the initial specification is incomplete.

#### 1.2.5 The Illustrated Models Are Examples of a Pattern

The three illustrated models for the business processes Sales, Purchase and
Labor Acquisition have many common features. They all model the trans-


<!-- Page 27 -->


14 1 Structural Patterns at Operational Level
actions between Joe’s Pizzeria and its trading partners as exchanges of
economic resources.

These models can be generalized into a model at a higher level of abs
traction, illustrated in the next chapter. The models for sale of pizza, purc
hase of raw materials and labor acquisition are examples of the REA
EXCHANGE PROCESS PATTERN.


<!-- Page 28 -->


### 1.3 REA Exchange Process Pattern 15

1.3. REA Exchange Process Pattern
ow’ ©
Pp P| en | oe,
x
nf
XK
\
Trade is the voluntary exchange of goods, services, or money
Context
You are an application designer developing a business application. You are
trying to create an object model of a business application and struggling to
find the right structure for the model and the right relationships between
entities in the model. You know the user requirements; they can be in a
written document or non-written information obtained by an ongoing dial
og with the users; but you know the requirements are incomplete. You
want to know the right questions to ask to better understand the application
domain. You also want the model to be consistent and robust enough for
future changes in user requirements.
Problem
How does one create a robust skeleton of an object-oriented model for int
eractions between the enterprise and it trading partners? User requirem
ents are not a sufficient source of information, because they are known to
be incomplete, often contradictory, and to change over time, and it is often
impossible to find what requirements are missing. Shortly, you would like
to create a business application that will satisfy even some of user req
uirements that have not been communicated to you.


<!-- Page 29 -->


16 1 Structural Patterns at Operational Level

Forces!

The REA exchange process pattern resolves the following forces:

e The modeled software application should provide information about
how the interactions between the enterprise and its trading partners
change the value of the economic resources of the enterprise. The applic
ation should keep track of the increases and decreases of the value of
the resources that are under the control of the enterprise, and should rec
ord which resources were exchanged for which others.

e Application designers want to concentrate on the fundamentals of the
users’ business, and separate those requirements which are likely to
change. The fundamentals are often so obvious to the users of business
applications that they do not communicate them, and they remain hidd
en until late stages of software development.

e The model should be consistent with the business domain rules. Applic
ation designers want to ensure that the model is consistent, complete,
and correct, with respect to the domain rules.

e Application designers want to include business semantics into the entit
ies in the application model. Semantics based only on the names of the
entities is not good enough because it relies on common knowledge, and
common knowledge is not available to software applications.

Solution

Model the interactions between the enterprise and its trading partners as

exchanges of economic resources.

Each exchange consists of at least one increment economic event that
increases the value of a resource of the enterprise by transferring rights to
the resource to or from other economic agents. Every increment event is
related to at least one decrement economic event that decreases the value
of a resource of the enterprise by transferring rights to the resource to or
from other economic agents. We call the relationship between the increm
ent and decrement economic events exchange duality, or in short, exc
hange. The exchange duality is a many-to-many relationship, indicating
that in the application model there must be at least one decrement for each
increment, and vice versa. Therefore, the exchange duality in the applica1
! In the pattern literature the term forces is used for the constraints that restrict

the solution of the problem, requirements, and properties that the solution

should have. Appendix C describes the pattern form in detail.


<!-- Page 30 -->


### 1.3 REA Exchange Process Pattern 17

tion model can be an n-ary relationship, that relates several increment and
decrement entities.

In order for an exchange process to add value, the overall increase in
value of the resources related to the increment events should be greater
than the overall decrease in value of the resources related to the decrement
events.

Each economic event is related to an economic resource, see Fig. 11.
The relationship between an increment and a resource is called inflow, the
relationship between a decrement and a resource outflow. In the applicat
ion model there must be exactly one economic resource for each econ
omic event, and at least one increment and one decrement for each econ
omic resource.

Each economic event is related to two economic agents. The economic
event in the exchange process transfers rights to the economic resource
from one agent to another. When the event occurs, the provider agent loses
rights to the resource, and the recipient agent receives the rights. In the app
lication model for each economic event there must be at least one prov
ider and at least one recipient agent. For each agent, there can be zero or
more economic events.

REA Exchange Process
{ar runtime, agents with } {a runtime, agents with }
different economic interests 1 1 different economic interests
renee Es ' ‘ Boye * receive
exchange
duality
Increment Event Decrement Event
1 { Piferen eeeates | 1%
Lee at runtime =
inflow outflow

i

Resource
Fig. 11. The REA exchange process

Note that the model in Fig. 11 determines the rules for constructing the
application model. The application model determines the structure of the
runtime instances.

The following domain rules apply for any application model describing
the REA exchange process.


<!-- Page 31 -->


18 1 Structural Patterns at Operational Level

Every increment economic event must be related by exchange dual-

ity to a decrement economic event, and vice versa.

Every increment economic event must be related by inflow relation-

ship to an economic resource.

Every decrement economic event must be related by outflow rela-

tionship to an economic resource.

Every economic event must be related by a provide relationship to

an economic agent, and by a receive relationships an economic

agent. At runtime, these two agents must represent entities with diff
erent economic interests.
Resulting Context
The domain rules in this pattern allow application designers to derive new
facts from the facts provided by the users, and to formulate questions leadi
ng to the discovery of new facts. Therefore, a business application can
meet most or all user needs, even if the user requirements and the designe
rs’ knowledge of the user needs are incomplete.

Note that at runtime, for some period of time, there might exist an ins
tance of an increment event that is not paired in exchange relationship
with a decrement event. This temporary imbalance results in a claim bet
ween economic agents. The claim can be materialized, for example as an
invoice. The concept of a claim is described in the chapter REA Exchange
Process in Detail, and the materialized claim is described as a pattern in

# Part II of the book.


<!-- Page 32 -->


### 1.4 REA Exchange Process In Detail 19


### 1.4 REA Exchange Process In Detail

In this section we explain semantics of the resources, events, agents, inf
low, outflow, exchange, provide, and receive, in the REA exchange proce
ss.

The purpose of the REA exchange process is to receive or give up

rights associated with economic resources by receiving or giving up

rights to other resources.

#### 1.4.1 Economic Resources

Economic resources are things that are scarce, and have utility for econ
omic agents, and users of business applications want to plan, monitor,
and control. This definition of a resource is common to both an exchange
and a conversion process?; however, the resources expose a different interf
ace to the exchange and conversion processes.

In the REA exchange process, a resource can be seen as a collection

of certain rights associated with it: ownership rights, usage rights,

copy rights.

Rights contribute to the resource value for an economic agent.
1.4.1.1 Rights Associated with the Resources
REA does not explicitly specify how to model the rights associated with
the resource. In this book, we follow the approach in which the inflow and
outflow relationships at the application model level determine the rights
transferred from one economic agent to another.

For example, Fig. 12 illustrates an REA application model for a Reader
of a Book (Fig. 15 models the same process from the perspective of Lib
rary). Economic resource Book is borrowed from Library. A reader can
identify what rights it has to the book by traversing the inflow and outflow
relationships related to the Book. In the model in Fig. 12 the Reader has
the right to read the Book, i.e., the Reader has the right to attach the Read
economic event to it. Precise specification of the “right to read” in the
REA framework, for example, whether it includes the right to borrow the
2 Please see the section REA Value Chain in Detail for discussion on economic

resources in general.


<!-- Page 33 -->


20 1 Structural Patterns at Operational Level
book for another reader, requires the concept of types, and can be exp
lained using the POLICY pattern.

REA Categories

provide
Economic 1 1..* Increment 0..* 4 F
Resource Economic Economic Agent
inflow Event 0." 1
REA Application Model «provide»
. Library
«economic resource» | 1 0..* «increment»
Bak «inflow» Borrow
ramoreee 0.."| borrower
«receive»
Fig. 12. Features and rights of the economic resource?

Note that there are alternative approaches to modeling the transfer of
rights than as instances of inflow and outflow. These approaches are outl
ined in the following section.
1.4.1.2 Alternative Approaches of Modeling the Rights
Researchers involved in modeling business systems using REA still disc
uss appropriate ways of modeling the rights. The other possible app
roaches to modeling rights are (Haugen 2005):

e Rights are properties of economic resources.

e Rights are components of economic resources, attached to the main res
ource to which they give rights.

e Rights are properties of inflow and outflow relationships.

e Rights are types of commitments (see the COMMITMENT PATTERN).

e Rights are refinements of the custody relationship between economic
agents and economic resources.

e Rights are defining characteristics of economic events.

3 Modeling conventions, and the correspondence between the model and

metamodel are outlined in Appendix B.


<!-- Page 34 -->


### 1.4 REA Exchange Process In Detail 21


All approaches have their advantages and disadvantages, and combinat
ions of these approaches may also be possible.


#### 1.4.2 Inflow and Outflow

In the previous section we described resource as a portfolio of rights, and
economic events in exchange processes transfer some of them.

Inflow is a relationship that relates economic resource with an in-

crement economic event. The enterprise receives some rights to the

resource as a result of the related increment event.

For example, as a result of a purchase economic event, the enterprise
will receive ownership rights, and during a rental economic event, the ent
erprise will receive rights to use the premises for the period of the rental.

Outflow is a relationship that relates economic resource with a decr
ement economic event. The enterprise loses some rights to the res
ource as a result of the related decrement event.

For example, after payment (an economic event), the enterprise will lose
ownership rights to money, and during rental, the owner will lose the
rights to use his premises for the period of the rental.

The inflow and outflow relationships and their cardinalities are illust
rated in Fig. 13. The model illustrates an Apartment that can be purchased,
rented or sold.

At the level of the REA categories (which describes how application
models are constructed), the inflow and outflow are one-to-many (1 to
1..*) relationships; one economic event is related to one resource, and a res
ource can be related to one or more economic events. For example, as ill
ustrated in Fig. 13, an Apartment is related to one increment and two decr
ement events. The enterprise receives ownership of Apartment (the
Purchase event), rents it (the Rental event) and terminates the ownership
(the Sale event). Rental is an economic event that lasts over an interval of
time; rights to use the apartment are transferred to the renter at the beginn
ing of the rental and returned at the end of the rental.


<!-- Page 35 -->


22 1 Structural Patterns at Operational Level
REA Categories
Increment inflow outflow Decrement
Economic Event 1.* 1 Resource 1 1.* Economic Event
REA Application Model
~~ «outflow»
«inflow» rights to use «decrement»
«increment» ownership «resource» 1 0..* eu
Purchase OR? Apartment
ee 7 «outflow» 9
e __-ownership
Establishes = [X Terminates = [X . ae
ownership ownership
Fig. 13. Inflow and outflow relationships
In the REA application model (which describes the construction of runt
ime entities), the inflow and outflow are one-to-many (1 to 0..*) relations
hips; one economic event is related to one resource, and a resource can be
related to zero or more economic events. An actual Apartment can over
time be rented, purchased, and sold zero or more times by the same econ
omic agent. Each Rental, Purchase, and Sale event is related to exactly
one Apartment. If several Apartments are rented to the same renter at the
same time for the same period, this would be modeled as several economic
events that occur simultaneously.
1.4.2.1 Inflow and Outflow are Linked to Actual Resources
An economic event is always related to the (actual) resource, which ref
lects the fact that a real thing is purchased or sold; an economic event’ is
never related to the resource type or group (see the discussion on TYPE
and GROUP patterns for definitions of type and group). A car dealer alw
ays sells a physical car, and guest always occupies a physical room in a
hotel. If an engineering company sells know-how and blueprints of a car to
a car manufacturer, we would model these artifacts as resources (not res
ource types) in this transaction. If a person buys a season ticket, e.g. all
Barcelona games in 2005-2006, the economic resources are the actual seats
when the Barcelona games are played, and the economic events are the
4 A commitment for an economic event can be related to a type. See the discuss
ion on COMMITMENT pattern.


<!-- Page 36 -->


### 1.4 REA Exchange Process In Detail 23

person’s actual attendances. For objects that consist of elements without
identity, such as water, gasoline, electricity, or money, the resources (res
ource instances) are the volumes limited by the scope of the economic
events. The section on resource types gives examples of instances of these
kinds of resources.

A resource type IN
may be related to
commitment
«resource type» In a well-formed REA application [\
Apartment Type | | model, there is never a relationship
ita ><- _~—-—4 between resource type and
“~. Leconomic event.
«specification» _
«resource» «outflow» «decrement»
Apartment Rent
Fig. 14. Economic event is always related to the actual resource
Nevertheless, software applications sometimes contain a relationship bet
ween economic event and resource type. For example, if an electricity
consumer is interested only in the total amount of delivered electricity, and
not in voltage, frequency and current at each moment in time, it is useful to
omit the electricity instance from the model, and relate the electricity sale
event and electricity resource type. Such a decision is a modeling comprom
ise, and the missing information is a trade-off for simplicity.

#### 1.4.3 Economic Events

Economic events in the exchange processes represent the permanent or
temporary transfer of rights to an economic resource from one economic
agent to another. The transfer of the rights represents increment or decrem
ent of the value of the resources.
The purpose of an economic event in the REA exchange process is
to transfer some of the rights associated with the resource from one
economic agent to another.
An increment event increases the value of the related resource, and the
decrement event decreases the value. An increment event does not always
mean that the enterprise should receive rights; for example, waste is a re-


<!-- Page 37 -->


24 1 Structural Patterns at Operational Level
source with negative value, therefore, by transferring ownership of waste
to the recycle station the enterprise increases overall value of its resources.
In the process illustrated in Fig. 15 the Lend economic event represents
a time-limited transfer of rights to the economic resource Book from Lib
rary to the Reader and back. The economic agent Library provides some
rights related to a copy of a book to the Reader. Reader receives the right
to borrow a copy of the Book, but not, for example, the right to sell this
copy or to create another copy of the Book. Reader has also not received
the right to substantially change the physical shape of the book, for examp
le, by excessive wear and tear, even though some wear is expected.
«agent» «agent»
Library Reader
lender borrower
«provide» «receive»
«outflow»
«resource» Lend «decrement» «exchange»
Reader receives the rights to [,
take a copy of the book outside
the library and read it, usually
for limited period of time.
Fig. 15. The event in the exchange process transfers rights associated with the res
ource
Lend is a decrement economic event in Library’s REA model, because it
restricts the Library’s rights to the Book during the Lend event. For examp
le, Library cannot lend the book to another Reader.
Economic events can IN
occur instantaneously or
«decrement» over period of time
Sale ee ‘Location in space of the [X
: 7 “ related economic resource
Date and Time - ___-------] during the event
Location in Space --7~
Quantity ---.0.
y —r----------- Equal to 1 for individually
identifiable resources
Fig. 16. Economic event in an exchange process


<!-- Page 38 -->


### 1.4 REA Exchange Process In Detail 25


The economic events address when economic agents had the rights to
the resources, and consequently when economic resources changed value.
If the economic resources can be located in space, the economic event also
determines where the economic resources changed their value.

The economic events in REA application models usually encapsulate
properties for Date and Time and Location in Space. As these properties
usually have specific behavior that differs from one application to another,
we describe them as behavioral patterns POSTING and LOCATION in the

# Part II of this book. The Quantity property determines the quantity or

amount of the resources for which the rights are transferred. The Quantity
property of the events related to the resources that are individually identifia
ble is always one, as there is one economic event for every resource unit.
Note that whether a resource is individually identifiable is often a decision
of the users of a business application. This topic is discussed in the chapter
REA Value Chain in Detail.

Date and Time and Location in Space are related; if we specify one of
them, we might often determine the other, and vice versa. As a result, alt
hough many economic events are determined by the time at which they
occurred, (for example, an employment event is often specified by its start
and end), many are also often determined by the resource’s location in
space. For example, economic agents can agree that the sale event (transfer
of ownership rights) occurs when goods are delivered to the customer; the
payment event occurs upon cash transfer between the bank accounts.
Typically, a contract between economic agents specifies when ownership
or other rights to a resource are transferred between them and how is it rel
ated to change of location.
1.4.3.1 Economic Events are Moments or Time Intervals
The economic events in exchange processes might occur instantaneously
or over an interval of time. For example, the sale of an apartment and its
payment occur instantaneously. The Sale event in Fig. 17 is a transfer of
ownership rights of the Apartment from the Seller to the Buyer. The
Apartment is owned by the Seller before the economic event Sale, and aft
er Sale the Apartment is owned by the Buyer.


<!-- Page 39 -->


26 1 Structural Patterns at Operational Level
At runtime, seller and buyer are entities
with different economic interests
«provide» ae
couttiow» seller ,
«resource» ownersnip | «decrement» ~*Y «agent»
Apartment Sale Person
«receive»
buyer
Apartment is under the control of the buyer
Apartment is under the control of the seller
time
SS
Fig. 17. Transfer of ownership can be an instantaneous event
The economic events that transfer rights other than ownership can occur
over a period of time, such as rental, loan, or lease; see Fig. 18. The Rental
event in this figure is a transfer of the right to use the Apartment from
Owner to Renter at the beginning of the Rental economic event. The
Renter has the right to use the premises for the duration of the Rental
event, and this right is returned to the Owner at the end of the Rental.
At runtime, Owner and Renter must be
entities with different economic interests
«provide» me
Rights to Use Owner -
«resource» g «decrement» r~—SY «agent»
Apartment Rental Person
«receive»
Renter
Apartment is under the control of the Renter
Apartment is under Apartment is under
the control of the Owner the control of the Owner
time
o_o
Fig. 18. Transfer of rights other than ownership is often a time interval
The sale of resources that cannot be individually identified, such as elect
ricity, water, or other fluid materials, as well as most services occur over a
period of time. The graph in Fig. 19 illustrates that the amount of electrici
ty changed ownership from seller to buyer over a period of time.
The rate at which resources change rights is not constant. For example,
in the middle of the graph in Fig. 19, there is a flat period in which no elec-


<!-- Page 40 -->


### 1.4 REA Exchange Process In Detail 27

tricity has been sold. Likewise, labor acquisition is an economic event that
occurs over the period of employment, but labor is acquired only during
working hours and not, for example, during weekends.

At runtime, Seller and Buyer must be
entities with different economic interests
«outflow» «provide» oN
«resource» Ownership «decrement» «agent»
Electricity Sale Company
«receive»
Buyer
Electricity is under the
Electricity is under the control of the buyer
control of the seller
time
iii‘
Fig. 19. Transfer of ownership can be a time interval
If an economic event in an exchange process occurs instantaneously, we
can deduce that this economic event transfers ownership between econ
omic agents. The opposite rule does not apply; transfer of ownership can
be instantaneous, as illustrated in Fig. 17, or occur over an interval of time,
as illustrated in Fig. 19.
1.4.3.2 Economic Events Occur in the Past or Present
A business application can register only economic events that have already
occurred or are occurring in the present. Economic events can certainly be
planned or expected to occur in the future; the REA concept of commitm
ent describes the events that have not yet occurred. A business applicat
ion can also register commitments for future economic events, and the fulf
illment relationship between commitment and economic event specifies
how good the prediction is. Commitments are described in the COMM
ITMENT PATTERN.
1.4.3.3 Increment Does Not Always Increase Value, and
Decrement Does not Always Decrease It
If the enterprise acquires the maintenance of equipment from a service
center, the acquisition is an increment economic event, because the value
of the equipment for the enterprise is usually higher after the maintenance
than before.


<!-- Page 41 -->


28 1 Structural Patterns at Operational Level

What if maintenance sometimes does not succeed, and the service center
damages the object to be maintained, and its value after the maintenance is
lower than before? If such a case has been specified by a contract (see the
CONTRACT PATTERN) between a service centre and the enterprise, the
REA model would contain a decrement event modeling the decrease of res
ource value, and would also contain an economic event for compensation.
If such a case has not been specified by a contract, then maintenance is still
an increment event, but the value of the resource is decreased. The econ
omic resources can change their value on their own, due to processes that
are not modeled.

Taking the previous example to the extreme, let us suppose that for the
car rental agent, renting a car to a celebrity increases the value of the car,
while renting it to anyone else decreases it. From the perspective of the car
rental agent, is rental an increment or a decrement economic event?

It depends on the usual business of the rental agent, and on what kinds
of changes in the resource value users want to plan, monitor, and control.
If the usual business is to rent cars to ordinary people, rental would be a
decrement economic event. If the car occasionally increases its value duri
ng rental, the application model will be the same, but this particular ins
tance of the decrement event will increase the value of the car. If the
rental agent’s usual business is to rent cars to both celebrities and others,
the model must contain two economic events: “ordinary rental” and “cel
ebrity rental.” As “celebrity rental” is an increment, it must be paired via a
duality relationship to some decrement event. Therefore, the model must
also specify what resources are used or consumed in relation with this “cel
ebrity rental” event.


#### 1.4.4 Exchange Duality

The exchange duality binds increment and decrement economic events tog
ether into an REA exchange process.

The purpose of the exchange duality is to keep track of which re-

sources were exchanged for which others.

Exchange dualities represents in the model why economic events occur.
For example, the pizzeria receives cash from the customer because the
customer gets his pizza. Conversely, the pizzeria gives the pizza to the cust
omer because the customer gives him cash. By asking (and answering)
“why do the economic events happen,” the REA domain rules help create a
complete and consistent business model. However, answers to some quest
ions, such as “why do we pay taxes,” is not always obvious. Examples of


<!-- Page 42 -->


### 1.4 REA Exchange Process In Detail 29

such models, and how they are answered by the exchange dualities, are ill
ustrated in the Modeling Handbook.

In the REA application model of an exchange process, every incre-

ment economic event must be related by an exchange duality to a

decrement economic event, and vice versa.

An example of an exchange duality is illustrated in Fig. 20.
REA Categories
exchange
Economic Event Economic Event
REA Application Model
«exchange duality»
Cash Receipt
“ Economic Event»
Delivery Service
Fig. 20. The exchange duality

The exchange duality at the REA category level (which describes how
to construct the application model) is a many-to-many (1..* to 1..*) relat
ionship, see Fig. 20. For example, a customer can pay (a Cash Receipt
event) for the Sale of an item and for receiving Delivery Service. The exc
hange duality must relate at least one increment event entity and one decr
ement event entity.

The exchange duality in the REA application model (which describes
constraints of the runtime entities) is a many-to-many (0..* to 0..*) relat
ionship. At runtime, several Sale events can be paid for by one check (an
actual Cash Receipt event), and one Sale can be paid in several installm
ents (several actual Cash Receipt events). However, there is no “must”
here; actual Sale can remain unpaid, usually for a period of time. Somet
imes, the Sale is never paid.

An application developer can restrict the cardinalities in the application
model; for example, if there is always a single payment for a single sale,
the cardinalities can be restricted to 0..1. If it then happens that the cust
omer pays for a sale with two payments, the software application will not
support it. Sometimes this might be a reasonable trade-off for simplicity.


<!-- Page 43 -->


30 1 «Structural Patterns at Operational Level

1.4.4.1 The Value of Exchanged Resources

Each resource that is subject to exchange has a different value for the econ
omic agents participating in the exchange. For rational economic agents,
an economic exchange can occur only if both economic agents perceive
the value of the received economic resources higher than the value of the
given resources; otherwise, they will not exchange them.

For example, see Fig. 21; the Sale is a transfer of ownership of Pizza
from Joe’s Pizzeria to Addy, and the Payment Receipt is a transfer of owne
rship of Cash from Addy to Joe’s Pizzeria. If Addy buys a Pizza at Joe’s
Pizzeria for $10, for Addy the Pizza has a value higher than $10; for Joe’s
Pizzeria $10 has a value higher than the Pizza. If Addy did not think that
the Pizza is worth $10 or more, and if Joe’s Pizzeria did not think that for
$10 it is worth selling the Pizza, the economic exchange would not occur.
Le., neither the economic event Sale nor the economic event Payment Rec
eipt would occur.

«economic agent» .
Joe’s Pizzeria: <economnic agent»
” Enterprise Addy: Customer
«receive»
«receive»
«provide»
«provide»
«decrement event» «increment event»
Sale «exchange» Cash Receipt
«outflow» | Date: May 5, 2006 Date: May 5, 2006 «inflow»
Quantity: 1 unit Amount: $10
«resource» \ i «resource»
: Pizza \ i iCash
For Addy, 1 Pizza IN For Joe’s Pizzeria, IN
has higher value $10 has higher value
than $10. than 1 Pizza.
Fig. 21. Resource has different value for each agent.

How does Joe’s Pizzeria evaluate how much Pizza is worth selling for?
Joe’s Pizzeria usually wants to sell the Pizza for a price higher than its cost
price. If Addy is an end customer (the one that consumes Pizza), his
evaluation of how much the Pizza is worth is often much less objective.
Addy’s immediate needs, the prices of Joe’s Pizzeria competitors, and a
discount of $3 from original price $13 to the new price of $10 may make
Addy believe that the Pizza is worth more than $10. It is also likely that the


<!-- Page 44 -->


### 1.4 REA Exchange Process In Detail 31

first pizza Addy buys has for him a higher value than a second pizza of the
same type, which explains why Addy in this hypothetical example buys
only one unit.

The purpose of the exchange duality is not to determine whether the
values of the related increments and decrements match (this is the purpose
of behavioral patterns). The only thing we can deduce from an exchange
duality in the REA model is that, for each participating agent, the overall
value of all increments is higher than the overall value of all decrements.

Users of business applications usually require more sophisticated funct
ionality of the exchange duality. The RECONCILIATION PATTERN des
cribed in Part II, Behavioral Patterns, can be used to identify which ins
tances of increment events correspond to which instances of decrement
events, and vice versa, for example to identify which sale events corres
pond to specific cash receipt events. The MATERIALIZED CLAIM
PATTERN can be used to determine the unbalanced value between the inc
rement and decrement events.
1.4.4.2 Time Order of Increments and Decrements
There is no logical constraint on the order of time in which the resources
will be exchanged. There can also be a significant delay between the occ
urrence of the increment and decrement economic events.

resource Decrement exchange Increment resource
Event Event
increment event (e.g. car rental)
No general rules about [\.
eee OCCU first
a decrement event (e.g. payment)
time
iii‘
Fig. 22. Increment and decrement events occur independently of each other

For example, a customer can pay before he can rent a car (see Fig. 22),
and vice versa. It is also quite common that one economic event occurs
during another one, for example, in the case of renting an apartment
against monthly payments. In such a case, one rental economic event
would be related through an exchange duality to several payment eco-


<!-- Page 45 -->


32 1 Structural Patterns at Operational Level
nomic events, some of which might occur before, some during, and some
after the rental.

If the economic agents would like to specify the desired time order of
the future economic events, they can specify it by commitments that are
part of the contract; see the discussion on CONTRACT PATTERN for det
ails.
1.4.4.3 Claim
Increment and decrement economic events in exchange processes usually
do not occur simultaneously. Whenever an economic event occurs without
the occurrence of all corresponding dual economic events, there exists a
claim between economic agents related to these economic events. A claim
is illustrated in Fig. 23.

If the values of the increment and decrement economic events are comp
arable, the Value of the claim can be obtained as the difference between
the values of the increment and decrement economic events. For example,
if the sale event specifies the price of the sold product, and cash is received
in the same currency, the value of the claim is simply the difference bet
ween these two monetary values. This example is illustrated in Fig. 23.

exchange
materialization i settlement
Fig. 23. Claim

However, the values of the increment and decrement event might not be
directly comparable. For example, the sale events might be linked to actual
products (there is one event per product unit), and the cash receipt event
represent the amount in a specific currency. If such a sale is partially paid
for, the claim between the sale and cash receipt events contains two kinds
of values: the quantities of the sold products and the value of the partial
payment.

In cases where the value of the increment cannot automatically be comp
ared with the value of the decrement, additional information can be re-


<!-- Page 46 -->


### 1.4 REA Exchange Process In Detail 33

ceived from commitments (see the discussion on COMMITMENT
PATTERN). This example is illustrated in Fig. 24.

, , exchange D ,
reservation reservation
:
fulfillment f fulfillment | Economic
Resource \ i Resource
: exchange i
Economic Event 7 pt Economic Event
\ 1 \ value to decrement v Z
N }, Value decremented <<],
Be value to increment
“~~~-—> value incremented
Fig. 24. Claim in a model with commitments

For example, the economic agents agree to sell 4 units of Pizza for $10;
the value of the decrement commitment is 4 units, and the value of the inc
rement commitment is $10. If the enterprise sells 3 units of Pizza and rec
eives $8, the values of the claim are 1 unit of Pizza and $2.

A claim is often materialized; i.e., users of business applications print a
document that states the value of the claim to a given date, and that often
contains additional information. Invoice is an example of a materialized
claim. As the methods to materialize the claim differ from one business
application to another (due to legislation in various countries and company
standards), we describe the MATERIALIZED CLAIM as a behavioral patt
ern.


#### 1.4.5 Economic Agents

Economic agents are the providers and recipients of the rights associated
with economic resources.


<!-- Page 47 -->


34 1 Structural Patterns at Operational Level
Economic agents in exchange processes are individuals or organizat
ions capable of holding the rights associated with economic res
ources, and of transferring or receiving these rights to or from other
individuals or organizations.
Examples of economic agents are enterprise, customer, vendor, and emp
loyee (in the labor acquisition process).
1.4.5.1 Contact Person
Sometimes, it is useful if the application model contains information about
a Contact Person, who is responsible for carrying out the economic event
for the trading partner. In these cases we assume that the trading partner
(such as Vendor in Fig. 25) delegated adequate responsibility toward the
contact person by the economic event Representation Service Acquirem
ent. As the enterprise has very little information about this delegation,
other than the fact that it exists, in many business software applications the
contact person is implemented as a property of the trading partner. This sol
ution is simple, but the full solution from Fig. 25 enables us to record
some properties of the responsibility delegation event, such as time period,
as well as to associate several contact persons with a particular trading
partner and economic event type.
The model Fig. 25 also illustrates, that sometimes it is useful to include
a part of business partner’s REA model in a solution, although the enterp
rise has limited information about it.
Vendor's REA Model Enterprise’s REA Model
«receiven «economic agent» «economic agent»
Vendor Enterprise
«provide» «receive»
«increment event»
Representation «decrement»
Service Purchase
Acquirement pe «inflow»
«provide» = N
Enterprise anticipates that «resource»
«economic agent» some event like this exists Product
Contact Person
Fig. 25. Vendor’s contact person


<!-- Page 48 -->


### 1.4 REA Exchange Process In Detail 35


#### 1.4.6 Provide and Receive

Provide and receive are relationships between economic agents and econ
omic events. Provide and receive relationships answer the question about
between whom rights are transferred, and, consequently, who has rights to
a resource at a given time.
entities with different
economic interests
1 1
provide receive
0..* 0..*
Fig. 26. Provide and receive relationships

A provide relationship in an exchange process determines the eco-

nomic agent who loses rights to the economic resource as a result of

the economic event.

A receive relationship in an exchange process determines the eco-

nomic agent who receives rights to the economic resource as a result

of the economic event.

During an economic event, rights to an economic resource are transf
erred from one economic agent to another. Therefore, there are exactly
two economic agents related to each economic event. In order to create a
complete model for the enterprise, we must specify for each economic
event which agent receives and which agent loses rights to the resource.
The following REA axiom specifies what the REA application models
should support.

In the REA application model, each economic event must be related

by a provide relationship to an economic agent, and by a receive re-

lationship an economic agent. At runtime, these two agents must
represent people or organizations with different economic interests.
In the trading partner view (see Appendix B), one of the agents is always
the enterprise, and the other agent is the economic agent to whom the en-


<!-- Page 49 -->


36 1 Structural Patterns at Operational Level
terprise transfers or from whom it receives some rights to economic res
ources.
«economic agent» «economic agent» «economic agent»
Addy Joe's Pizzeria Library
«provide» «receive» «provide»
«receive»
«decrement event» «exchange duality» «increment event»
Sale Cash Receipt
«outflow» «inflow»
«resource» «resource»
Pizza Cash
Fig. 27. Joe’s Pizzeria sells pizza to Addy and receives payment from Library

In most cases we illustrate so far, the increment economic event has the
same providing and receiving agents as the decrement economic events rel
ated to it by the exchange duality. In the Sales process of Joe’s Pizzeria,
the Customer and Joe’s Pizzeria are related to both the increment and decr
ement economic events. However, actual agent participating in the increm
ent and decrement events can be different. In the example illustrated in
Fig. 27; Joe’s Pizzeria sells Pizza to Addy, and receives payment from Lib
rary; the Sale event has different participating agents than the Cash Rec
eipt event.

This exchange process must still add value for all participating agents. Is
it worth it for Addy to get Pizza paid by Library? Well, if it would not be
the case, the Addy would not receive Pizza, Joe’s Pizzeria would not sell
it, or Library would not pay for it.


<!-- Page 50 -->


### 1.5 How Joe’s Pizzeria Obtains Pizza 37


### 1.5 How Joe’s Pizzeria Obtains Pizza

, a
-
. -

The REA EXCHANGE PROCESS pattern does not apply, because Joe’s
Pizzeria does not obtain pizzas from its trading partners

#### 1.5.1 Producing Pizza

Joe’s Pizzeria produces pizza from Raw Materials such as dough, peppero
ni, tomatoes and cheese, by using an Oven and by consuming Labor. The
process of producing pizza is essentially a conversion (transformation) of
the Raw Material, Labor (the worked hours) and the Oven (the time when
the oven has been used) into a Pizza, see Fig. 28. The Raw Materials bec
ome part of Pizza, they are consumed during production. Employee’s Lab
or is also consumed; the time when the employee has worked on pizzas is
gone when the pizza is finished, and is not available anymore. On the other
hand, the Oven can be used again, although it might need some cleaning
and maintenance after a Pizza has been baked.

In principle, there are also other resources required to produce pizza,
such as the kitchen in the building in which Joe’s Pizzeria is located, heati
ng of the building, and maintenance of the oven. Joe has decided he is not
interested in tracking how they are transformed into each Pizza. Therefore
we do not model them as economic resources in this process.

Labor ;
Oven
Fig. 28. The pizza production process


<!-- Page 51 -->


38 1 Structural Patterns at Operational Level
The REA model for pizza production is illustrated in Fig. 29. The Mater
ial Issue, Labor Consumption, and Oven Use are decrements of resources,
because they decrease the value of the Raw Material, Labor and Oven. The
Pizza Production is an increment event, because it creates a new resource
with a positive value.
«economic agent» «economic agent» «economic agent»
Supervisor Cook Waiter
| «provide» | ‘ 7
«provide» «provide» i ee ieceiven «provide»
| «receive»
«resource» «decrement»
«increment» «produce»
«decrement» Pizza
habe or » «consume» Labor Production
Consumption cS
«conversion»
Oven Oven Use . Pizza
«receive»
«use»
Fig. 29. The REA model for the pizza production
The economic resources Raw Material, Employee Labor and Oven are
under the control of the employees Supervisor, Cook and Waiter. The emp
loyees physically control the resources on behalf of Joe’s Pizzeria, but
they do not own them, neither do they have any legal rights to these res
ources; the model in Fig. 29 illustrates that the economic agent Supervisor
issues the Raw Material to the agent Cook, who bakes a Pizza and passes it
to the Waiter. The Supervisor also provides Oven to the Cook to bake a
Pizza. To explain who controls Labor requires deeper analysis (see the
section on Labor in the Modeling Handbook): the Supervisor controls
Cook’s Labor, he assigns a task to the Cook; the Cook consequently takes
of the control of his Labor and consumes it to produce a Pizza.

#### 1.5.2 Summary

The REA model focuses on the core economic phenomena and abstracts
from technical aspects of the conversion. This has several advantages.


<!-- Page 52 -->


### 1.5 How Joe’s Pizzeria Obtains Pizza 39


The model answers the question as to which economic resources have
been used, consumed and produced during the process. The economic
events provide the information on when, where and how the changes of the
resources occurred, and the economic agents provide the information on
who controlled the economic resources during these changes. This is the
information the business decision makers need in order to plan, monitor
and control the economic resources.

The REA model does not imply any restrictions on the time order in
which the economic events occur. If the users of a business application
wish to specify the desired order of events, the model can be extended usi
ng commitments (described in the SCHEDULE PATTERN) to specify
when the events should occur. However, the model can still record what
actually happened, and thus determine the difference between the schedule
and the actual production.


#### 1.5.3 The Pizza Production Process is an Example of


a Pattern

In addition to producing pizza, Joe’s Pizzeria performs additional activities
in order to keep the company running. Cleaning of the restaurant and
maintenance of the equipment are the examples. If Joe schedules the pizza
production in order to purchase the right amount of raw materials, or if he
has an accountant who keeps his financial books, the planner’s and acc
ountant’s labor are transformed into the services that, as their end result,
make Joe’s Pizzeria a better company. The cleaning, maintenance, plann
ing, accounting are essentially conversions of labor and tools into other
economic resources.

The pizza production, and the abovementioned processes are examples
of a pattern, the REA CONVERSION PROCESS.


<!-- Page 53 -->


40 1 Structural Patterns at Operational Level

### 1.6 REA Conversion Process Pattern


x I

am - ; aa. ee
r a |
: BSB gian 's

Photo by Ulrik de Wachter
Conversion is a physical, structural, or design change or transformation
from one state or condition to another
Context
You are an application designer developing a business application. Among
the business processes of the enterprise, there usually are one or more
processes that create new products or services, or add value to the existing
ones. These processes might be specified by the users of a business applic
ation, but you know the user requirements are incomplete. You want to
know the right questions to ask to better understand the application dom
ain. You also want the model to be consistent, and robust against future
changes in user requirements.
Problem
How does one create a robust skeleton of an object-oriented model for a
business process that creates new products or services, or adds value to the
existing ones? User requirements are not a sufficient source of informat
ion, because they are known to be incomplete, often contradictory, and to
change over time, and it is often impossible to find out what requirements
are missing. In short, you would like to create a business application that
will satisfy even some of the user requirements that have not been comm
unicated to you.


<!-- Page 54 -->


### 1.6 REA Conversion Process Pattern 41


Forces

The solution to this problem is influenced by four forces.

e The model should provide information about how the process of creati
ng and modifying resources influences their value, and when the value
has been changed.

e The model should provide information about who was responsible for
the resources and when.

e The model should capture the fundamentals of the users’ business, and
filter out those user requirements that are likely to change.

e The model should be consistent, complete, and correct with respect to
the business domain rules.

Solution

Model the process that creates new products or services or adds value to

the existing ones as a conversion of some economic resources to others.

During the conversion, the enterprise uses or consumes economic re-

sources in order to produce the resources of the same or another kind.

Each conversion consists of at least one increment economic event that
increases the value of the resource by modifying its features, and at least
one decrement economic event that decreases the value of a resource by
modifying its features. The increments and decrements in the conversion
processes typically occur over a period of time.

Each increment event is related to exactly one economic resource by a
relationship called produce. The produce relationship means that the econ
omic event creates a new economic resource or modifies some features of
an existing resource. Each decrement event is related to exactly one econ
omic resource either by a use or by a consume relationship. The consume
relationship means that the economic resource does not exist after the decr
ement event (the resource is consumed). The use relationship means that
the economic resource still exists after the decrement event, but some of its
features have been modified.

In order to keep track of which resources have been used or consumed
in order to produce others, the increment and decrement economic events
are related by the conversion duality relationship, or in short, conversion.
The conversion duality is an n-ary relationship; in the application model
there can be many increment and many decrement events related by a sing
le conversion duality.


<!-- Page 55 -->


42 1 Structural Patterns at Operational Level
Each economic event is related to two economic agents. The economic
event in the conversion process transfers the control over the economic res
ource from one agent to another. Each event is related to exactly one econ
omic agent by a provide relationship, and to exactly one economic agent
by a receive relationship, see Fig. 30. The transfer of control can occur at
the beginning, at the end or during the economic event. Each agent can be
related to zero or more economic events.
REA Conversion Process
1 1
| ror
receive provide v / provide .
0..* 0..* receive
conversion
duality
Decrement Event Increment Event
— ie 1 0.*
{ either or ro eoneuiie
i 1 produce
use Economic
1 Resource 1
Fig. 30. REA conversion process
In order for a conversion process to add value, the overall increase in
value of the resources related to the increment events should be greater
than the overall decrease of value related to the decrement events, over the
period reflecting the entrepreneurial goals of the enterprise.
The following domain rules apply for any REA application model des
cribing the conversion process.
Each increment economic event must be related by a conversion dua
lity relationship to a decrement economic event and vice versa.
Each increment event must be related by a produce relationship to
an economic resource.
Each decrement event must be related either by a use or by a cons
ume relationship to an economic resource.


<!-- Page 56 -->


### 1.6 REA Conversion Process Pattern 43


Each economic event must be related by both provide and receive

relationships to an economic agent.

Resulting Context

The domain rules in this pattern allow application designers to derive and
discover new facts from the facts provided by the users of a business app
lication. Therefore, a business application can meet most or all fundam
ental user needs, even if the user requirements and the designer’s knowle
dge of users’ needs are incomplete.

Note that at runtime, for some period of time, there might exist a decrem
ent event that is not paired in conversion duality with an increment
event. For example, the oven must be turned on good time before the baki
ng of pizza can start.


<!-- Page 57 -->


44 1 Structural Patterns at Operational Level


### 1.7 REA Conversion Processes in Detail


In this chapter we explain the semantics of the resources, events, agents,
use, consume, produce, conversion duality, provide, and receive, in the
REA conversion process.

The purpose of the REA conversion process is to create new eco-

nomic resources or to change features of existing resources by using

or consuming resources of the same or another kind. Economic

events in the conversion processes can change the values of the fea-

tures, as well as add and remove features to and from the resources.

#### 1.7.1 Economic Resources

Economic resources are things that are scarce, and have utility for econ
omic agents, and users of business applications want to plan, monitor,
and control. This definition of a resource is common to both an exchange
and a conversion process*; however, the resources expose a different interf
ace to the exchange and conversion processes.

In the REA conversion process, a resource can be seen as a collec-

tion of certain features associated with it.

Features are properties, characteristics, capabilities or states of a res
ource that establish the utility of the resource for an economic agent:
pizza has a certain weight, size, packaging, taste, vitamins and minerals
content, is delivered on time, is freshly baked, and is known from TV.
These features contribute to the resource value.

REA does not explicitly specify how to model the features of the res
ource. Some features can be modeled as properties of the resource, such
as the weight of a pizza, some as relationships to other REA entities, such
as the freshness of a pizza determined by the end of the Pizza Production
event. In Part II of this book we model the features of the resources as
modules of functionality called aspects. In the REA application models,
the names of the produce and use relationships can indicate the features of
the resource expected to be modified by the related economic event.

Fig. 31 illustrates an example of the resource Pizza, created during econ
omic event Baking (i.e. this event changes the Existence feature of the
5 Please see the section REA Value Chain in Detail for discussion on economic

resources in general.


<!-- Page 58 -->


### 1.7 REA Conversion Processes in Detail 45

Pizza). The increment event Packing changes the Packaging feature of the
Pizza. The decrement event Issue for Packing temporarily (i.e. during the
duration of this event) changes the Availability feature, e.g. when issued
for packing, a Pizza cannot be used for other purposes. The decrement
economic events for usage and consumption of the resources needed to
bake the pizza are omitted in the diagram for simplicity.
REA Categories
Resource Decrement
produce conversion
1 use Te
a consume 1."
REA Application Model | Creates pizza IX Temporarily changes|\.
(changes the "existence” feature) the “availability”
é feature. E.g. during
«produce» . . :
; «increment» packing the pizza
SxIsierice Baking cannot be eaten.
Pizza * ¥
i Existence ee
i . 1 availability
|; Packaging 0..*
{Ly Availability
Features IN d «increment» «conversion» | decrement»
specified «produce» 0..* Packing 0. 0..* | Issue for Packing
informally packaging... |
Changes the “packaging” feature IN
Fig. 31. The conversion process changes features of the resource

In the REA software application, it is necessary to store the features on
the resource entity, while the rights an economic agent has to the resource
are determined by relationships to the economic events. The main reason is
that while rights can be transferred only by economic events, resource feat
ures can change on their own, as a result of the processes that are not part
of the application model.

Features can change on their own, not only as a result of economic
events in the application model. The reason of this fact is that the applicat
ion model does not need to contain all conversion processes that might aff
ect the features of the resource. If the features of the resource change on
their own, it usually means they have been changed by some conversion
process, which the application designers have not modeled. This is natural;
model is not an exact copy of the world, but contains only the information
relevant for the model. Therefore, features, and consequently also values


<!-- Page 59 -->


46 1 Structural Patterns at Operational Level
of resources can change as a result of the processes that are not part of the
application model.

Existence is one of the features of the resource; the only feature that the
resource must have. This seems obvious for real-world objects. However,
software applications do not contain real-world objects; they contain inf
ormation about real-world objects, therefore, there is often need to keep
information about the resources that do not exist, that have been, for exa
mple, consumed or destroyed. The fact that a resource ceased to exist
does not mean we delete a record of this resource from a database, but just
note its non existence. Existence has obviously an essential impact on the
value of the resource.

If a resource receives new features, and loses some existing features, the
consequence might be that it changes its type. The TYPE PATTERN des
cribes this concept in detail.


#### 1.7.2 Produce, Use and Consume


In the previous section we described resource as a portfolio of features,
and economic events change some of them. The Produce, Use and Cons
ume relationships between the resource and the economic event repres
ents the features of the resource that are intended to be changed by the
economic event.

Produce is a relationship that relates economic resource with an in-

crement economic event. The enterprise intends to increase the

value of the resource as a result of the related increment event.

Produce means both creation of the resource, such as baking a pizza
from raw materials, and improvements to the resource, such as packing a
pizza. Maintenance and transport are other examples of the inflow econ
omic event with a produce relationship.

Consume is a relationship between an economic resource and a dec-

rement economic event. After a decrement economic event, the re-

source is entirely used up and does not exist after the event ends.

For example, the flour and water are consumed during the pizza product
ion process; see Fig. 32.

Use is a relationship between an economic resource and a decrement

economic event. After the decrement economic event, the resource

still exists, and its value may be unaffected.


<!-- Page 60 -->


### 1.7 REA Conversion Processes in Detail 47


For example, an oven still exists after the pizza production process in
Fig. 32.

The use relationship does not specify anything about the value or the
economic resource after the related decrement event. The economic event
is a decrement because the value of the resource for the enterprise becomes
smaller during the event. For example, the economic event might someh
ow restrict the utilization of the resource: the oven used for a pizza prod
uction in Fig. 32 may not at the same time be used for other purposes.
However, after the event, the value can be the same as before; other typical
example is playing a CD in a CD player. In other cases, the value of the res
ource is smaller after the decrement event, and after a certain number of
decrement events the value of the resource can become zero or even negat
ive; after many uses, the enterprise might decide to transfer the oven owne
rship to the recycle station.

The Pizza Production process illustrated in Fig. 32 is an example of a
conversion process with produce, use and consume relationships.

At the REA category level (which describes how application models are
constructed), the produce, use and consume are one-to-many (1 to 1..*) rel
ationships. For example, as illustrated in Fig. 32, the enterprise consumes
the resources Flour and Water, uses the resource Oven, and produces the
resource Pizza, which is then transported to the Customer using the res
ource Vehicle. All economic events last over an interval of time.

In the REA application model (which describes the construction of runt
ime entities), the consume is a one-to-one (1 to 0..1) relationship; one decr
ement event is related to one resource, and a resource can be related to
zero or one decrement event. An actual volume of Flour and Water can be
added at most once (then they do not exist), each Flour Addition and Wat
er and Flour Mixing is related to a specific volume of Flour and Water.

The use is a one-to-many (1 to 0..*) relationship; one decrement is rel
ated to one resource, and a resource can be related to zero or more decrem
ents. An actual Oven can over time be used zero or more times, and each
Oven Use is related to exactly one Oven.

The produce relationship which creates a resource is a one-to-one (1 to
0..1) relationship; one increment event is related to one resource, and a res
ource can be related to zero or one increment event. An actual Pizza can
be created at most once, and each Pizza production produces exactly one
Pizza.

The produce relationship which modifies an existing resource is a
many-to-many (1..* to 0..*) relationship; one increment event is related to
one or more resources, and a resource can be related to zero or more inc
rements. For example, an actual Pizza can be Transported zero or more
times, and each Transport economic event is related to one or more Pizzas.


<!-- Page 61 -->


48 1 Structural Patterns at Operational Level
REA Categories consume
1 1. conversion produce
Resource Decrement 11 Increment Resource
5 . . 1..* 1
1 set
REA Application Model
«consume»
«resource» | ©X!stence | ¢qecrement»
Flour 1 0..1| Flour Addition |g *
«produce»
«consume» : «increment» existence
«resource» | existence | “Cecrement» 0..* Pizza
Water and 0." Production | °--!
Water 1 0..1| Flour Mixing c>
KEOnsSUme» 0..*” / «conversion»
1 existence «decrement» 1

### 0.41 Water and 1

ES” Yeast Mixing «resource»
state of . Pizza
«resource» | being used| «decrement» | 0-- 1
Oven 1 0..*| Oven Use se
state of being transported «produce»
0..* location
«decrement» aincrament
: 0..* A Pizza
On Vehicle 0. Tenepart 0*
«USE» >
«resource» wear out | <decrement» «conversion»
Vehicle 1 0.* Drive (hee
Fig. 32. Produce, use and consume relationships
This example also illustrates that the users’ viewpoint determines what
the economic resources are. If users are also interested in how making
Pizza affects resources such as ingredients, tools, and kitchen, these res
ources must be included in the model. This process also produces waste;
if the users are interested in modeling the produced waste, the waste
should also be included in the model.

#### 1.7.3 Economic Events

Economic events in the conversion processes represent changes to the feat
ures of the resources, and the transfer of control of an economic resource
from one economic agent to another. The changes to the features represent
increments or decrements of the value of the resources.


<!-- Page 62 -->


### 1.7 REA Conversion Processes in Detail 49


The purpose of an economic event in the conversion process is to

create or consume a resource, or to change some of the features of

an existing resource.

An increment event increases value of the related resource, but it does
not mean that every actual event must increase the value; the increment
events increase the overall value of the resources over the period reflecting
the entrepreneurial goals of the enterprise. The same applies for the decr
ement events.

The economic events address when the resource features have been
changed, when economic resources changed value, and when economic
agents had the resources under their control. If the economic resources can
be located in space, the economic event also determines where the econ
omic resources changed their value.

The economic events in the conversion processes do not transfer rights
to the resources between economic agents. If a resource has been created
in the conversion process, the enterprise has ownership rights to this res
ource by default. If a resource has been consumed, enterprise loses the
ownership and no other agent can receive rights to the resource that does
not exist.

Pizza Production over period of time Flour Consumption
(Sid Location in space of the [\X od ~ Date and Time

Location in Space ---}-----------+ related economic resource |--------------+- Location in Space

to 1 (each event produces one unit of pizza)
Fig. 33. Economic event in a conversion process

The economic events in REA conversion processes usually occur over a
period of time. The properties for Date and Time and Location in Space
typically have behavior that differs from one application to another, we des
cribe them as behavioral patterns POSTING and LOCATION in the Part II
of this book. The Quantity property determines the quantity or amount of
the used, consumed or produced resources. The Quantity property of the
events related to the resources that are individually identifiable is always
one, as there is one economic event for every used, produced or consumed
resource unit. Whether the resources are individually identifiable is often a
decision of the users of a business application. For example, if Joe does not


<!-- Page 63 -->


50 = 1 «Structural Patterns at Operational Level
want to keep track of each individual Pizza, the Quantity property of the
Pizza Production event in Fig. 33 would be a natural number different than
1, and the resource related to this event would represent an identifiable (by
Joe) set of pizzas, such as the pizzas produced during a period of time.
Please see also the discussion in the chapter REA Value Chain in Detail.
1.7.3.1 Economic Events are Time Intervals
The economic events in conversion processes usually occur over an interv
al of time. For example, in Fig. 34, the On Vehicle is a decrement event
representing the time interval when an /tem is on a truck, and the Transp
ort event the time interval when the /tem is actually changing its location.
«use» «conversion»
placed on vehicle «decrement» transport «increment»
On Vehicle Transport
«resource» «produce» location
Item
Item is placed on vehicle
On Vehicle event:
Item is transported
time
iii‘
Fig. 34. Events are usually time intervals

As economic events in conversion processes usually occur over a period
of time, it is useful to specify when exactly the participating economic
agents transfer control over the resource. The answer is different for res
ources that can be individually identified (such as cars) and resources that
cannot (such as fuel).

Transfer of control over resources with individually unidentifiable elem
ents, but whose identity is determined by their quantities, such as fluid
resources and some services, occurs continuously during their use, produce
and consume economic events; see Fig. 35. For example, production or
consumption of electricity occurs continuously, and the transfer of control
over electricity from the distributor to the customer is continuous.


<!-- Page 64 -->


### 1.7 REA Conversion Processes in Detail 51

«agent»
4 «provide» Distributor
«resource» «produce» «decrement»
Electricity Produce ,
«receive». «agent»
Consumer
Electricity is under ee Electricity is under
the control of distributor a the control of the consumer
decrement event
tine >
Fig. 35. Transfer of control over resources that cannot be individually identified
From the model in Fig. 35 we can determine that the provider economic
agent controls the resource before the economic event, and that the recipie
nt agent controls the resources after the economic event. During the econ
omic events, the provider agents control some amount of the resource and
the recipient agent some other amount.
«agent»
«provide» Supervisor
«resource» «consume» «decrement»
Raw Material Consume .
«receive». «agent»
Worker
Raw material is under Raw material is under . .
the control of the provider the control of the recipient Raw material ceased to exist
SS
decrement event
SS Time —_—_ >
Fig. 36. Transfer of control occurs at the beginning of the consumption of an indiv
idually identifiable resource
For individually identifiable resources the answer is more specific, and
depends on whether the relationship is use, consume, or produce.


<!-- Page 65 -->


52 1 Structural Patterns at Operational Level

The provider agent transfers control to the recipient agent at the beginn
ing of an economic event that consumes resources; see Fig. 36. For exa
mple, a warehouse clerk gives control to the production worker over raw
material that will be consumed during production at the beginning of the
event that consumes the raw material. Likewise, an employee receives
control over his own labor as soon as he starts working on a task given by
the supervisor.

The provider agent has control over an economic resource before and
after an economic event that uses the resource, and the recipient agent has
control over the resource during the event; see Fig. 37. For example, if a
production worker needs special tools to perform a production operation,
the warehouse clerk has control over the tools before and after the econ
omic event, and the production worker has control over the tools during
the event that uses the tools.

«provide» Warehouse Clerk
«resource» ause» «decrement»
Tool Use
«receive» «agent»
Tool is under the control Tool is under the control Tool is under the control
of the provider of the recipient of the provider
decrement event
"OT ATATTTTNTNT7T7T7T711 &@£_>
Fig. 37. Transfer of control during use of an individually identifiable resource

The provider agent transfers control to the recipient agent at the end of
the economic event that produces the resource. For example, a production
worker gives control to the warehouse clerk over the finished product
when the product is complete; see Fig. 38.


<!-- Page 66 -->


### 1.7 REA Conversion Processes in Detail 53

«providew Worker
«resource» «produce» «increment»
Produce Produce ;
«receive». «agent»
Warehouse Clerk
Product is under the control Product is under
of the provider the control of the recipient
increment event
JA ACTEM _$£{_>
Fig. 38. Transfer of control at the end of production of an individually identifiable
resource

#### 1.7.4 Conversion Duality

The conversion duality binds increment and decrement economic events
together into an REA conversion process.
The purpose of the conversion duality is to keep track of which res
ources were used or consumed in order to produce others.

Conversion duality represents in the model why some resources are used
or consumed. For example, the pizzeria uses oven and consumes raw mater
ials and labor because it produces pizza.

The following REA axiom specifies what the REA application models
should support.

In the REA application model of a conversion process, every increm
ent economic event must be related by a conversion duality to a
decrement economic event, and vice versa.

An example of a conversion duality in the process of disassembling a
bicycle is illustrated in Fig. 39.


<!-- Page 67 -->


54 1 Structural Patterns at Operational Level

REA Categories

conversion
REA Application Model
«conversion duality»
«decrement» 0..* Disassembly 9g « «increment»
ss
Labor Consumption Frame Extraction

Fig. 39. The conversion duality

The conversion duality at the REA category level (which describes cons
traints of the application model) is a many-to-many (1..* to 1..*) relations
hip, see Fig. 39. For example, a mechanic can consume his labor (the Lab
or Consumption event) and a bicycle (the Issue Bicycle event) for the
Wheel Removal and for the Frame Extraction events. The conversion duali
ty must relate at least one increment event entity and one decrement event
entity.

The conversion duality in the REA application model (which describes
constraints of the runtime entities) is a many-to-many (0..* to 0..*) relat
ionship. At runtime, typically two Wheel Removal events occur for one
Frame Extraction event.
1.7.4.1. The Value of Produced, Used and Consumed

Resources
In the conversion process, the enterprise use or consume resources in order
to produce other resources. The increment economic event either creates a
new resource unit, or increases the value of an existing resource by changi
ng some of the resource’s features. In return, the decrement economic
events use or consume the enterprise’s resources.

The overall incremented value of the produced resources (considering
the enterprise’s entrepreneurial goals) should be higher than the overall
decremented value of the consumed or used resources. This statement is
true only on average. A specific production run can be unsuccessful and
the overall value of the resources is decreased. However, on average the


<!-- Page 68 -->


### 1.7 REA Conversion Processes in Detail 55

process must add value; otherwise, a rational enterprise would not perform
this process.
1.7.4.2 Time Order of Increments and Decrements
There is a logical constraint on the order of time in which the resources are
used, consumed, and produced in the conversion process. Usually the inc
rement event starts after or at the same time as some decrement event
starts, and ends before or at the same time as some decrement event ends
(resources cannot be produced from nothing); see Fig. 40.

«Use» or
«Consume» «Decrement «conversion
Event» duality»
«Use» or
«Consume» «Decrement - «increment «Produce»
Event» Event»
4 A decrement event
before
NS i A decrement event
after /
BN lV An increment event
tine NNN
Fig. 40. Time constraints on conversion processes

#### 1.7.5 Economic Agents

Have you ever witnessed a situation in which an administrative assistant of
a department is running around asking colleagues, “Who ordered this
package?” This situation can occur when the enterprise receives an item,
and probably also holds the legal rights to this item, but the physical cont
rol of this item is held by an employee, and the business application of the
enterprise or vendor, or both, is missing information about which person
should physically control the item.


<!-- Page 69 -->


56 1 Structural Patterns at Operational Level

Economic agents in conversion processes are individuals (not or-

ganizations) capable of controlling economic resources, and of

transferring or receiving the control to or from other individuals.

Examples of economic agents in conversion processes are employees (in
the labor consumption process), and people providing services for the ent
erprise.

In the conversion processes, the agents related to economic events can
transfer to each other control of the resources, but cannot usually have
ownership or other legal rights to these resources. These agents bear the
responsibility for the resources on behalf of the enterprise or of other
agents.

Therefore, the economic agents in the conversion process do not need to
be entities in the legal sense; the agents are always physical people. We
can say that, in general, economic resources are always controlled by
physical people or machines. However, sometimes it is not possible or
relevant to include them in the application model. In such cases the econ
omic agents can be an organizational unit, such as team, department, or
even enterprise (as organizational unit, not as legal entity). The meaning of
this modeling compromise must be specified by the application designer; it
could mean, for example, that “someone from the department” has control
over the economic resource.

The economic agent that holds the rights to the economic resources can
be different from the economic agent that physically has the economic res
ource under its control. For example, equipment and tools are owned by
the enterprise, but they are physically under the control of the employees
that work with the equipment and use the tools.

Likewise, a single economic agent can participate in both exchange and
conversion processes. For example, an employee has rights to his labor,
which he exchanges with the enterprise for financial compensation. Simult
aneously, the employee physically controls some of the resources of the
enterprise, because he participates in the enterprise’s conversion processes.

#### 1.7.6 Provide and Receive

Provide and receive are relationships between economic agents and econ
omic events. Provide and receive relationships answers the question
about between whom control is transferred, and, consequently, who cont
rols a resource at a given time.


<!-- Page 70 -->


### 1.7 REA Conversion Processes in Detail 57


A receive relationship in a conversion process determines the eco-

nomic agent who receives control over the economic resource as a

result of the economic event, but has no legal rights to the resource.

A provide relationship in a conversion process determines the eco-

nomic agent who loses control over the economic resource as a re-

sult of the economic event, but has no legal rights to the resource.

During an economic event, the control over an economic resource is
transferred from one economic agent to another. Therefore, there are exa
ctly two economic agents related to each economic event. In order to crea
te a complete model for the enterprise, we must specify for each econ
omic event which agent receives and which agent loses control over the
resource.

In the REA application model of a conversion process, each eco-

nomic event must be related by a provide relationship to an eco-

nomic agent, and by a receive relationships an economic agent.

In conversion processes, the provider and the recipient can be the same
agent, for example, in cases where the same economic agent is responsible
for consecutive business processes. For example, if an economic agent
Cook is the only employee in Joe’s Pizzeria, he would be both provider
and recipient in the economic events Material Issue and Pizza Production.
1.7.6.1 Rights to the Resources in Conversion Processes
The purpose of conversion processes is to change the features of the res
ources, not to exchange the rights to the resources. The enterprise holds
the rights to the created, used, and consumed resources. In a liberal legal
system, the enterprise owns the resources it creates. Likewise, if resources
are consumed, the enterprise loses its rights to these resources.

In the REA application model, the enterprise holds the rights to the

resources the enterprise produces, uses and consumes.

An enterprise can by contract with other economic agents commit itself
to transfer to them ownership or other rights to the resources at the mom
ent they are created or acquired by exchange. For example, employees
might during employment produce intellectual property, which we model
as an economic resource. Employees that create intellectual property own


<!-- Page 71 -->


58 1 Structural Patterns at Operational Level
it, but many companies have a clause in their employment contracts acc
ording to which employees transfer to the company their intellectual
property, for example, protected by patents, that they produce during the
employment period. Such a transfer would be modeled as an economic
event in the scope of the labor acquisition process, see Fig. 41. For the Ent
erprise, the increment events are the receiving rights of employee’s Labor
and the receiving rights to employee’s Intellectual Property from the Emp
loyee, and the decrement event is the Salary Payment.

Enterprise «provide» Employee

«receive» .
Labor Acquisition | , exchange». Salary Payment
«inflow» <S «outflow»
«increment event» «resource»
arecelve> «provide» Cash
Intelectual

Intellectual «inflow»

Property
Fig. 41. Acquiring intellectual property
1.7.6.2 The Enterprise Does Not Always Controls Its

Resources

The enterprise does not always control the resources it has rights to; the
economic agents that control the resources of the enterprise do not necess
arily act on behalf of the enterprise. Typical examples are services that are
provided by other agents to the enterprise’s resources, such as transport,
maintenance, and outsourced manufacturing operations.

The model in Fig. 42 illustrates an example of maintenance of an enterp
rise’s Equipment. The enterprise acquires a maintenance service from a
service provider by economic event Maintenance Acquisition. During econ
omic event Maintenance Consumption, the enterprise, which “owns” the
service when it is acquired, passes control over this service to the Service
Provider agent, who consumes it in order to perform the Maintenance
economic event. The Service Provider is the economic agent who controls
the enterprise’s Equipment during the Maintenance economic event. At the


<!-- Page 72 -->


### 1.7 REA Conversion Processes in Detail 59

end of the Maintenance event, the Equipment is again under the control of
the Enterprise.

«agent» «receive «agent»
Service Provider —— «receive» Enterprise
«provide» «provide»
«inflow» «increment» «exchange» «decrement» «outflow»
Maintenance Cash
Acquisition Disbursement
«resource»
Maintenance «resource»
Service Cash
«consume» «decrement» —} «conversion»
Maintenance
Consumption <> «produce»
F Maintanance
«provide»
«use» «decrement» «resource»
Release for Equipment
Maintanance
«provide» «receive» receive» «provide» «receive»
«agent» «agent» «agent»
Enterprise Service Provider Enterprise
Fig. 42. Equipment is not under the control of the enterprise during maintenance
1.7.6.3 Who Has Changed The Features Of The Resource?
The economic agents participating in the economic events during convers
ion processes are not necessarily the same as the agents that changed the
features of the resources. The agents that changed the features of the res
ource are those whose labor has been consumed during the process.

For example, in the model in Fig. 43, the economic event Painting
changes a feature of a Product. The economic agent Supervisor has control
over the Product before, during, and after the Painting event. The econ
omic agent that changed the feature of the product was Painter, because
his Labor has been consumed in the Painting.


<!-- Page 73 -->


60 1 Structural Patterns at Operational Level
Warehouse Clerk Painter Supervisor
«provide» «receive» «provide» «receive»
Paint Material Issue Painting
Ss «produce»
a «conversion»
Labor Consumption
Product '
paints Product Product Issue :
«receive» «provide»
wane ene nn nanan nana d----------------------| Product under
«receive» his control
Painter Supervisor
Fig. 43. Painter has painted the product under supervisor’s control
The concepts of providing and receiving control are related to the conc
ept of custody; see the separate discussion on CUSTODY PATTERN. Cust
ody is a responsibility for the resources of the enterprise given to an econ
omic agent; for example, a warehouse clerk is responsible for the items in
the warehouse. The difference between custody and responsibility is that
custody can be established, transferred, and cancelled independently of the
economic events in conversion processes. Economic agents who have cust
ody for the enterprise’s resources can be different of the agents whose
services are consumed in conversion processes that affect these resources.
For example, a manager of gas station has custody over the fuel in the und
erground tanks, but the process of disposing of the fuel is provided by
other agents, often the customers in self-service gas stations.


<!-- Page 74 -->


### 1.8 Value Chain of Joe’s Pizzeria 61


### 1.8 Value Chain of Joe’s Pizzeria

\ ~ - _ Ec sala N
SS ee Re
-_ ‘ x ri E a
| 7 Ke |
Each business process utilizes resources generated by other processes
So far, we identified several exchange and conversion processes of Joe’s
Pizzeria, the Sales, Purchase, Labor Acquisition, and Pizza Production. At
the output of each process there is an economic resource that is an input of
another process, see Fig. 44.
Pizza «exchange process»
Sales
«conversion process»
Raw Material
Labor
«exchange process» |_ «exchange process»
Labor Acquisition Purchase
Fig. 44. Value chain of Joe’s Pizzeria

The Pizza Production process produces Pizza, which is exchanged in
the Sales process for Cash. Joe’s Pizzeria uses Cash to purchase Raw Mat
erials and Labor in the Purchase and the Labor Acquisition processes.
The Raw Materials and Labor are consumed to produce Pizza in the Pizza
Production process.

Each business process in Fig. 44 can be modeled using the economic
resources, events, agents, and, if needed, also the commitments, contracts,
and other entities that we introduce later in this book. This expansion is
symbolically illustrated in Fig. 45.


<!-- Page 75 -->


62 1 Structural Patterns at Operational Level
CE)
Pizza — See —
SS,
Pizza Production
| | = Cash
=)
L\
Raw Material
> Labor
Labor Acquisition urchase
[| |} | Ftd
Fig. 45. Value chain with expanded processes

Modeling the value chain helps the application designer to get an overv
iew over the business processes of the enterprise and has several other
advantages.

Firstly, it helps to identify the economic resources, by specifying which
things the users of a business application want to plan, monitor and cont
rol.

Secondly, it helps to find possible omissions in the REA models. For
example, the REA model for the Pizza Production process, illustrated earl
ier in Fig. 29, uses the resource Oven. The Oven is not related to any inc
rement economic event, therefore the model violates one of the domain
rules, and the complete model in Fig. 45 cannot explain how Joe’s Pizzeria
receives and loses the rights to use the Oven. To resolve this problem, an
application developer can either remove the resource Oven from the model
(and, consequently, Joe will not be able to track its value using the softw
are application), or add a process with an increment event related to the
Oven. Joe can also decide to leave the model inconsistent (we call it a
modeling compromise), but it will be a rational and qualified decision (not
an omission) and Joe will be aware of its consequences.

The model in Fig. 45 is created from Joe’s Pizzeria’s point of view. For
every exchange process in Joe’s Pizzeria’s REA model, there must be a
corresponding exchange process in the REA model of Joe’s Pizzeria’s
trading partner. For example, in the REA model of the Customer, there
must be a Purchase process with the events Purchase and Cash Disbursem
ent, corresponding to the Sales process of Joe’s Pizzeria. Likewise, in the


<!-- Page 76 -->


### 1.8 Value Chain of Joe’s Pizzeria 63

REA model for the Employee, there must be a Labor Provision process
with the events Labor Sale and Cash Receipt, corresponding to the Labor
Acquisition process of Joe’s Pizzeria, see Fig. 46.
Customer's
Processes «exchange process»
Cash Purchase Pizza
Pizza Cash
Joe's Pizzeria
Processes ; «exchange process»
Pizza Sales
Cash
«conversion process»
Pizza Production
Raw Materials
Labor
«exchange process» «exchange process»
Labor Acquisition Purchase
Employee’s Gash Vendors Cash Raw
Processes = Labor Processes Materials
«exchange process» Raw «exchange process»
Labor Labor Sales . Materials Sales \
Fig. 46. Semantics of exchange processes
We can generalize Joe’s Pizzeria’s chain of business processes into a
pattern, REA VALUE CHAIN.


<!-- Page 77 -->


64 1 Structural Patterns at Operational Level

### 1.9 REA Value Chain Pattern

GRY 4 ; 3
Ce a 4 Ihe a as = . a 2
tho i = a os

Growing grapes, aging the wine and testing quality are the main valuea
dding processes of a winemaker
Context
An enterprise creates value by developing and providing goods and serv
ices customers desire. Goods and services are created through a series of
business processes monitored and controlled by users with the support of
one or more business applications. For example, an enterprise can use one
business application for production and manufacturing, another application
for warehouse management, and yet another application for sales, distribut
ion and finance.

Problem

Application designers would like to model business processes of the enter-

prise in a way that would enable them to create an REA model for each

process, with an option to implement each REA model as an independent
software component. However,

e They are not able to identify all the resources that users of business app
lications would like to manage, monitor, and control. At what level of
granularity should the resources, and, consequently, the REA models,
be?

e Application designers have already created REA models for several
processes, but would like to relate them together, get an overview of the
whole model and eventually identify missing processes.


<!-- Page 78 -->


### 1.9 REA Value Chain Pattern 65


Forces

The solution to this problem is influenced by four forces.

e The business process model should be independent of the technology
the customer uses, and should rather describe fundamentals of the users’
business. As the implementation technology often changes the sequent
ial order of processes and events, the relationships between processes
and events should be expressed as logical constraints rather than as seq
uential order. The software solution should cover any sequence physic
ally allowable, and only restrict the order by business rules configura
ble at runtime.

e On the other hand, the business process model should be precise enough
to be compatible with the REA model, i.e., it should be possible to ref
ine this model to an object-oriented application model expressed by res
ources, events, and agents, from which a software application can be
generated.

e If each business process will be implemented as an independent softw
are component or an application, the components and applications
must have well defined interfaces that enable them to communicate.

e There are several methods for modeling business processes, such as
IDEFO, Porter’s value chain, flow charts, organization charts, and workf
lows, but none of them is sufficiently compatible with REA.

Solution

Model an enterprise as a chain of value-adding business processes that in-

fluence the value of the resources, which users of business application

want to plan, monitor and control. Inputs to each business process are the
resources used or consumed by the business process or given away to other
economic agents; outputs of each business process are the resources prod
uced by the business process or obtained from other economic agents.

Both the exchange and conversion processes accomplish the business ob-

jective of adding value to the resources that are under the control of the en-

terprise, over the period reflecting the entrepreneurial goals of the enterp
rise.

Process Process
Fig. 47. The REA value chain


<!-- Page 79 -->


66 1 Structural Patterns at Operational Level

The resources that are inputs and outputs to business processes should
be the resources that users of business applications want to plan, monitor,
and control. This determines the level of detail of the model.

The REA value chain consists of three modeling elements: REA Conv
ersion Process, REA Exchange Process and Resource Value Flow.

An REA conversion process is a process that uses or consumes the

resources that are under the control of the enterprise, and produces

new resources or changes some of the features of existing resources.

Examples of a conversion process are a manufacturing operation and a
service operation such as transportation.

An REA exchange process is a process that transfers some rights to

the enterprise’s resources to other economic agents, and receives

some rights to other resources in return.

Examples of an exchange business process that transfers ownership
rights are the sales and purchase processes; examples of processes that
transfer other rights, such as usage rights, are financing, labor acquisition,
and insurance.

A resource value flow is a relationship between REA processes.

This relationship represents the resource input and output of a proc-

ess. The direction indicates that of the value flow; the process at the

beginning of the flow (the end without an arrow) adds value to the
resource; the process at the end of the flow (the end with an arrow)
takes away value from the resource.

Each resource value flow must start and end in some business process;
no “loose ends” are allowed for resource value flows in well-formed mode
ls. This does not mean that at runtime the resource cannot just appear and
disappear due to unexpected events that are not part of the model; but this
is not the usual way in which an enterprise creates value. The value chain
describes the usual (not exceptional) way in which how enterprise creates
value; more precisely, it describes the processes that users of a business
application want to plan, monitor, and control.

Each resource value flow must start and end in some business proc-

ess. Each business process must have an incoming and an outgoing

resource value flow.


<!-- Page 80 -->


### 1.9 REA Value Chain Pattern 67


The resources that come from outside the enterprise or leave the enterp
rise are modeled as inputs and outputs of the exchange business proce
sses.

A single resource can be both input and output of a single business
process. For example, cash is both the input and the output of the financing
business process; an item is both the input and the output of the quality ass
urance process.

An REA business process is either an exchange process or a convers
ion process.

The statement above specifies that there are no “mixed” business proce
sses whose responsibility would be both to change features of the res
ource and transfer rights between economic agents. In such a configurat
ion, a business application would leave out some information about
economic resources.

Process for Creating a REA Value Chain

As the first step in creating the value chain of the enterprise, it is helpful
to think about the context of the enterprise. To whom does the enterprise
give resources and from whom does it receive resources? The result can be
something similar to that diagrammed in Fig. 48.

Cash___-

Pizza

(— Customer
Cash
+
t Joe’s Pizzeria
rd
Ingredients and
Vendor Raw Materials LO
Cash
Labor—
Employee
Fig. 48. Business context of the enterprise

Customer buys Pizza that the Enterprise produces, which is an exchange
of Pizza for Cash. Vendor gives the Enterprise Ingredients and Raw Mat
erials in exchange for Cash. Employees provide the Enterprise their labor
in exchange for Cash. A context diagram for the enterprise similar to the


<!-- Page 81 -->


68 1 Structural Patterns at Operational Level
one in Fig. 48 is useful as a starting point in identifying the company’s res
ources.

The second step in creating an REA value chain is to identify the busin
ess processes of the enterprise; an example is in Fig. 44.

The exchange business processes of a pizzeria would be the Sales proce
ss that exchanges Pizza for Cash with the Customers, the Purchase proce
ss that exchanges Raw Material for Cash with the Vendors, and the Labor
Acquisition process that exchanges Labor for Cash with the Employees.

The enterprise typically also has one or more conversion processes, in
which it produces or adds value to the product or service that it sells to the
customers, and in which it consumes or uses the resources obtained from
the vendors and employees. The conversion process of the pizzeria is the
Pizza Production process that produces Pizza from Raw Material and lab
or.

The third step in creating a value chain is to hierarchically decompose
the business processes to find the resources which the users of a business
application would like to plan, monitor, and control. The level of detail at
which to stop the decomposition is determined by the needs of the users of
a business application. The REA value chain should be decomposed to the
level at which the users of a business application need information to plan,
monitor and control the resources of the enterprise. This level varies from
one company to another. For example, for most companies it is sufficient
to know the total amount of cash in the treasury, but in some cases keeping
track of all the coins and the bills is required.

" « »
Raw Material
Schedule Labor Trasport Service
Labor Acquisition Purchase
Planning Transport
Fig. 49. Value chain with supporting processes


<!-- Page 82 -->


### 1.9 REA Value Chain Pattern 69


The fourth step in creating the value chain is to identify the rest of the
business processes, such as planning, marketing, accounting, human res
ources, and legal services, and to add them to the enterprise’s value chain.
These processes consume the resources of the enterprise, but using tradit
ional modeling techniques it is not always a trivial task to determine what
value they add. The REA framework helps analyze the purpose of these
processes, and how they add value to the enterprise’s resources.

Fig. 49 illustrates the value chain with two more processes: Planning,
which consumes Labor to assure that all resources needed to produce Pizza
are available, and Transport, which consumes Labor and Equipment to del
iver Pizza to the Customers. The Part III of this book is devoted to these
modeling issues.

«exchange process»
Leasing
«conversion process»
Raw Material
Schedule Labor Trasport Service
«exchange process» «exchange process»
Labor Acquisition Purchase
Labor Labor Equipment
«conversion process» «conversion process»
Planning Transport
Fig. 50. Value chain after the consistency check

The fifth step in creating the value chain is to consolidate the model
with the REA models for each process, and to assure that the model does
not violate the domain rules. For example, a model for Pizza Production in
Fig. 29 contains a resource Oven related by a use relationship to a decrem
ent event Oven Use. As every economic resource must be related to both
an increment and a decrement event, an application designer might decide
either to remove the resource Oven from the model, or to add an increment
event related to the Oven. Joe told the application designer that Joe’s Piz-


<!-- Page 83 -->


70 ~=1 Structural Patterns at Operational Level

zeria has a leasing contract for the Oven. Leasing is essentially an exc
hange of the Oven for Cash, and Fig. 50 illustrates the value chain with
the Leasing process.

Resulting Context

An application designer is focused on processes that add value to the final
products and services directly, as well as on supporting processes. A softw
are solution may need to support some of these processes, others may be
manual.

Due to well-defined resource interfaces between business processes, app
lication designers can design a different business software application for
each business process. Therefore, the REA value chain determines the syst
em level architecture of the business software solution. Implementing
each REA process as an independent software component makes the softw
are solution more adaptable to unanticipated changes in the customer’s
business.

The REA value chain ignores the time sequence of the processes, which
is exactly what we want to achieve for design purposes. We know that the
time sequence is very volatile, and in reality many of these processes occur
concurrently. At this point we would like to concentrate on the purposes of
the processes and the economic resources that are their inputs and outputs.

A drawback of this approach is that for a reader it might be difficult to
find a place to start reading this diagram. Probably, the easiest way to start
reading the diagram is to identify a natural end of the value chain, i.e., the
sales process, or if it is not there, some exchange process equivalent to
sales, usually a process whose output is cash and whose the input is a
product or service; for example, for a municipality library, this can be a
process of lending books. Then, continue reading the diagram backwards
through the value chain to the processes, whose output is the product or
service being sold, and find the input resources to these processes; and so
on.


<!-- Page 84 -->


### 1.10 REA Value Chain in Detail 71


### 1.10 REA Value Chain in Detail

In this section we explain semantics of the economic resources, and REA
exchange and conversion processes.
The purpose of the REA value chain is to link together REA models
into a chain of value-adding processes, and define the interfaces bet
ween them.

The REA Value Chain is a network of business processes whose purp
ose is to directly or indirectly contribute to the creation of the desired feat
ures of the final product or service, and to exchange it with other econ
omic agents for a resource that has a greater value for the enterprise in its
perception of its entrepreneurial goals.

The REA value chain model does not describe sequences, steps, and
tasks of the business processes. Time sequences of activities vary often
with technology changes, but the changes in sequence typically do not
change the fundamental way in which the process adds value. Therefore,
the value chain model focuses on the core phenomena of the business, and
abstracts the time sequences that change often.

The time sequence is given indirectly in the form of logical constraints.
For example, the conversion process Pizza Production cannot start unless
the resources Labor and Raw Materials are available.


#### 1.10.1 Resource Value Flows

Economic resources are the inputs and outputs of the REA exchange and
conversion processes.

In order to create a complete model for the enterprise, we must specify
for each resource how the enterprise obtains rights to it, for example, how
is it received, or produced, and how the enterprise loses rights to it, i.e.,
how it is consumed, or given away.

An enterprise can receive rights to a resource by producing it in a conv
ersion process, or by receiving it in an exchange process. In the REA
model, it is indicated by a produce or an inflow relationships between the
resource and an increment economic event, respectively.

An enterprise can lose rights to a resource by consuming it in a convers
ion process, or by giving it away in an exchange process. In the REA
model, it is indicated by consume or outflow relationships between the res
ource and a decrement economic event. The enterprise does not lose


<!-- Page 85 -->


72 1 Structural Patterns at Operational Level

rights to a resource by using it in a conversion process, as the resource ex-

ists after the economic event related to the resource by a use relationship.
In the REA application model, every economic resource must be rel
ated to at least one increment event by an inflow or produce relat
ionship, and to at least one decrement event by an outflow, use, or
consume relationship.


#### 1.10.2 Economic Resources


Economic resources represent the values that users of a business applica-

tion seek to control.
Economic resources are things that are scarce and have utility, that
are under the control of an economic agent, and that users of busin
ess applications want to plan, monitor, and control.

Examples of economic resources are products and services the enterp
rise provides, money, and raw materials, tools, and services the enterprise
uses and consumes.

Things we call economic resources must be scarce, not readily available
at no cost, such as air, or sea water by the seashore. Antarctica ice is an
economic resource in Europe, but not in Antarctica.

The users of business applications are important in the definition of
economic resource: the perspective of the users of a business application
determines which economic resources are modeled. As different users are
interested in different resources, the REA application model must contain
economic resources for all intended users of the application.

Value of an economic resource for an economic agent is determined by
the rights the agent has to the resource, and by the features of the resource.
An agent can change its rights to the resource by an exchange process; and
the features by a conversion process
1.10.2.1 Quantity of the Resource
Economic resources typically have a property Quantity that indicates
whether and how much of the resource is under the control of the enterp
rise. For example, the quantity of the economic resource cash indicates
the amount of cash that the enterprise has under its control. This amount
can consist of owned and borrowed cash. If we would like to know how


<!-- Page 86 -->


### 1.10 REA Value Chain in Detail 73

much is owned and how much is borrowed, we need to examine the econ
omic events that are related to the economic resource cash.

Quantity for discrete items that can be identified as individual units,
such as cars and buildings, is always measured in pieces and may have
values 1 and 0, indicating whether the item is, or is not, under the control
of the enterprise. Quantity for resources that cannot be individually identif
ied, such as screws, gasoline, electricity, and work, is measured with an
appropriate unit such as kilogram, liter, joule, or hour. Occasionally, we
might come across discrete items that can be split into smaller parts, such
as pizza. We model a process of cutting pizza into slices as a conversion
process, which produces several units of a new resource “a slice of pizza,”
each with quantity 1.

Setting the value of quantity to 0 means that the resource is not under
the control of the enterprise, and that the enterprise wants to keep informat
ion about this resource in its software system. For example, the resource
has been sold and the enterprise is bound by guarantee or service agreem
ent to the new owner of this resource, or the resource has been consumed
or destroyed, and the enterprise has to keep record of this resource for rep
orting or statistical purposes.

The property quantity is different than property quantity on hand. Quant
ity on hand is usually a property of a resource group, see the GROUPING
PATTERN, and is a non-negative integer for discrete items, and a real
number for the resources that cannot be individually identified.
1.10.2.2 Value of the Resource
The value of the resource indicates how much the resource is worth to the
economic agents that are related to it via economic events or commitm
ents. The value of the resource depends on four factors:

e On the features of the resource, we discussed the features in the Convers
ion Processes in Detail chapter.

e On the rights an economic agent has to the resource; we discussed the
rights in the Exchange Processes in Detail chapter

e On the economic agent related to this resource via economic events and
commitments. As the resource can be related, via economic events and
commitments, to different economic agents simultaneously, it might
have (and typically has) a different value for each economic agent. For
example, goods in trade have different values for the seller and the
buyer.

e On how the resource is used or potentially can be used by the economic
agents. The actual use is specified by related economic events, and po-


<!-- Page 87 -->


74 ~~ 1 Structural Patterns at Operational Level

tential use by commitments (see the COMMITMENT pattern for details).

As the resource can be related to several economic events and commit-

ments simultaneously, a resource might have different values for a sin-

gle economic agent at the same time. For example, a car has a different
value (and consequently a different price) for the agent, in the case he
intends to rent it, than in the case he intends to sell it.
: Quantity can be 0 or 1 for individually IN
«economic resource» identifiable resources (indicating whether
ect
«value» Cost -----t--7~7~] semantics of Cost.
Fig. 51. Value and quantity of the economic resource

Although for the reasons mentioned above the resource can have several
values simultaneously, it is useful to model the resource value as an prope
rty on an economic resource, as illustrated in Fig. 51; but since such a
value is a derived (calculated) property, the model must specify how the
value is obtained, or it might otherwise be interpreted incorrectly. For exa
mple, the value can reflect the cost of the resource for the enterprise, or
the price in the case of sale of the resource, or the price in the case of
rental of the resource.

The value of the resource is variable in time and can change on its own,
not only as a result of economic events in the application model. For exa
mple, the value of food or medicine rapidly decreases after expiry date.
However, the enterprise does not know the exact value of such expired
food or medicine, until they dispose of or consume it, perhaps for other
purposes than originally intended. Therefore, the full explanation of this
phenomenon requires a notion of economic contract or schedule, and its
evaluation; we intend to return to the evaluation of contracts in the addend
um to this book on the Internet.

The value of some resources can be negative, for example, the value of
toxic waste: to dispose of toxic waste decrements the company’s resources.

The value of the economic resources is often affected by economic
events that are unknown at the time users of business applications want to
estimate the value. For example, the precise value of goods on stock (the
sale price) is unknown until the goods are sold. Therefore, the value of the
economic resources must often be estimated, considering the entrepreneur
ial goals of the enterprise. Estimation of the value of the resources theref
ore encompasses considering the resources’ transformation in the enterp
rise’s value chain, and then estimating the price of a contract with a


<!-- Page 88 -->


### 1.10 REA Value Chain in Detail 75

potential customer. As this is difficult to do in the real-world, most res
ources use two value attributes: cost and unit price, both substituting the
real resource value.
1.10.2.3 Cost and Unit Price
The cost indicates the aggregated value of the decrement events from exc
hange processes directly or indirectly related to the resource. For examp
le, maintenance (an economic event) increases the cost of equipment;
more precisely, the cost of equipment is increased by the aggregated value
of the economic resources that have been used and consumed during maint
enance.

The cost of a resource is often affected by economic events that occur
continuously and will be registered in the future (such as the heating of
buildings). Therefore, various estimation methods are used to determine
the approximate cost of the resource.

In some business applications, resources have attributes unit price or list
price. They hold the suggested price of the resource in the sales process;
more precisely, they determine the default price of the resource in sales
contracts to unknown customers. Resource can have these attributes for
convenience, but they are not an intrinsic part of the REA application
model.
1.10.2.4 Modeling Ad Hoc Resources
A company might sometimes sell ad hoc resources or services that are not
registered in their business applications. Some business applications allow
for typing free text on an invoice, such as “miscellaneous,” with a price. In
this case the economic event has not been related to any resource, and, furt
hermore, the “miscellaneous” resource instance has not been created in
the business application. This is an example of modeling compromise. This
might be convenient in some software applications, but an application des
igner must be aware of its consequences. It would not possible to create
reports on the “miscellaneous” resources, and the reports on “standard”
products might be incorrect. A better solution would be to create a “misc
ellaneous” or “unspecified” resource group and relate it using the outflow
to an economic event, see Fig. 52. This is also a modeling compromise, but
the application will be more consistent, and allow for better reporting than
by omitting the outflow relationship from the model.


<!-- Page 89 -->


76 ~=1 Structural Patterns at Operational Level
«resource group» 1 0..* «decrement»
Miscellaneous «outflow» Sale
Fig. 52. Miscellaneous or unspecified resource (modeling compromise)
1.10.2.5 Individually Identifiable Resources
Economic resource in the REA model is an actual unit, which reflects the
fact that a real thing is produced, used, consumed, purchased or sold. The
resources whose units are individually identifiable, have, or in principle
may have, a serial number or its equivalent, see Fig. 53. The
IDENTIFICATION behavioral pattern discusses this concept in detail.
«resource» «resource»
Car Pizza

Fig. 53. Individually identifiable resources

Whether the resources are individually identifiable is often a decision of
the users of a business application. For example, Joe might decide that he
does not want to keep track of each individual Pizza. In this case, the Pizza
entity in Fig. 53 would represent a set of pizzas, such as the pizzas prod
uced in a period of time, or other identifiable set, which Joe is interested
in planning, monitoring and controlling. Screws and nails are other examp
les of the resources, which, in principle, are individually identifiable, but
which users of business applications often do not want to plan, monitor
and control individually. Such resources are modeled as resources with ind
ividually unidentifiable elements, described in the following section.
1.10.2.6 Resources with Individually Unidentifiable Elements
Resources such as money, bulk or fluid material, consist of individually
unidentifiable elements; for example, molecules in gasoline or grains in
pizza flour are not individually identifiable. Such resources are identified
by the volumes of material in the scope of the economic events, such the
volumes of material related to sales, production, and transportation. For
example, the volume of gasoline produced during a certain time interval is


<!-- Page 90 -->


### 1.10 REA Value Chain in Detail 77

identifiable, and flour is delivered in bags, which are identifiable. Packages
of screws can be assigned unique numbers. There might be legal reasons to
identify and register them — in the food and chemical industries it is usual
to keep samples of raw materials from the delivered bags.

Heating Oil as a Resource

Fig. 54 illustrates two REA models for supplying heating oil from a truck
to a house tank. The upper part illustrates the heating oil supplier viewp
oint; the bottom part illustrates the household (customer) viewpoint. The
conversion processes in Fig. 54 model the movement of heating oil is from
a truck into a house oil tank. The exchange processes model the sale
(change of ownership) of the Supplied Heating Oil.

There are three identifiable instances of heating oil: Heating Oil in
Truck Tank, Supplied Heating Oil, and Heating Oil in House Tank. Supp
lied Heating Oil is the oil that actually flows through the pipe from the
truck to the house tank. It is a transient resource, consumed at the same
time as it is created: the supplied heating oil is created by removing it from
the truck’s pipe, and it is consumed by mixing it with the oil already pres
ent in the house tank. After the supplied oil is in the house tank, it is not
possible to distinguish the supplied oil that came from the truck from the
oil that was already in the house tank.

Money as a Resource

Coins and bills are identifiable entities; therefore, they are (actual) res
ources. Money in a bank account is also an (actual) resource, but it does
not have individually identifiable elements. Therefore, coins and bills on
one side, and money in a bank account on the other side must be modeled
as different kinds of resources. Fig. 55 illustrates withdrawal of Cash, an
exchange process between Bank and Customer; the Customer gives to the
Bank an amount of Money from his bank account, and receives Coins and
Bills in return. Some banks charge a fee for this transaction, which is also
illustrated in Fig. 55. The increments and decrements are from customer’s
perspective.


<!-- Page 91 -->


78 1 Structural Patterns at Operational Level
Heating Oil Supplier REA model
«agent» «resource»
Driver Heating Oil in
«provide» Truck Tank
Customer «provide» «receive»
:
«receive» i
«decrement» eeoneuinee j
. «conversion»| Remove Oil from saul :
«increment» Truck Tank i
Supply Oil : N
«resource» «decrement»
Supplied Use Pump&Pipe
Heating Oil Oil transferred |X.
; |_| throught the pipe, «use» «resource»
«outflow»
«decrement» | «exchange»| «increment» | “iMflow»| resource»
Sales Cash Receipt Cash
Household REA model
«agent» «provide» «agent»
Customer Supplier
«receive» sprovida> «receive»
«increment» eacererent «outflow» «resource»
Purchase «exchange» qe Cash
inflows Disbursement
«resource»
Supplied :
Heating Oil o Semeiored IN «resource»
_ throught t le pipe, «produce» Heating Oil in
oo «conversion» | «increment»
House Tank Fill House Tank
. «recipient» ; Supplied i ; N
«provider» «provide» upplied oil plus
the oil that already
«agent» «agent» was in the tank,
Customer «receive» Driver e.g. 6000 litres
Fig. 54. Fluid materials as resources


<!-- Page 92 -->


### 1.10 REA Value Chain in Detail 79

«provide»
«agent» day. «agent» «receive»
Bank «provide» _ Customer
«receive» «receive»
. «resource»
«receive» «provide» ——_ ]- «inflow» Coin
«provide»
«decrement» «exchange» «increment»
«resource» Withdrawal oe || Coin Receipt
Money in
Account cS
Account number Exchange Fee «increment» sill
Bill Receipt
«outflow» Charge
Bill number
«inflow» Value
Fig. 55. Coins, bills, and money in an account as resources
Labor as a Resource
We can think of labor instance as having a specific identity, which consists
of the identity of the person providing the labor, and the time and place the
labor is provided. A labor also has a length (acquired amount in hours or
days) specified by the Acquire Labor event; this is similar to the volume of
resources with individually unidentifiable elements.
- «agent» «agent» «agent»
period when [\ Enterprise Employee Supervisor
labor has been ;
consumed ~ «provide» «receive» «provide» qrocalven
«consume» ~ S adecrement» ; «increment» «produce»
Gon sume Labor «conversion» Production
Item
employee's specific skills «increment» «exchange» «decrement»
aquired amount Acquire Labor Pay Salary
; tf
i — «receive rovide» | «resource»
period for which [X._ “Provide» «recevey » Cash
labor has been
acquired «agent» «receive»
Employee Enterprise
Fig. 56. Labor as a resource


<!-- Page 93 -->


80 1 Structural Patterns at Operational Level


#### 1.10.3 Alternative Models of Business Processes


There are several other methods for modeling business processes, such as
IDEFO, Porter’s value chain, various forms of flow charts, and organizat
ion charts. Any of these methods is entirely compatible with REA.

Application designers could use a function modeling method, such as
IDEFO (Integration Definition for Function Modeling, 1993), designed to
model the activities, decisions, and actions of an enterprise. Activities are
related by their inputs, outputs, controls, and mechanisms; see Fig. 57.
IDEFO activities can be hierarchically refined into models with greater det
ail. IDEFO is not intended to be used for modeling sequences, which is
good from an REA perspective; but the method is not intended to help und
erstand how the activities add value to the economic resources.

calle
Input — Output
Activity
a“
Fig. 57. IDEFO activity is less specific than the REA Processes.

If we compare the IDEFO activity with an REA business process, IDEFO
inputs and outputs correspond to the REA economic resources consumed
and produced by the IDEFO activity. IDEFO mechanisms are people, mac
hines, or systems that orchestrate the transformation of inputs to outputs.
Some mechanisms are REA economic agents, and some are REA econ
omic resources. Controls regulate or constrain the output of the activity.
IDEFO controls correspond to REA policies, i.e., entities that relate tog
ether types of economic agents, events, and resources.

The major advantage of IDEFO — that it is a general modeling technique
— is also a drawback from an REA perspective, because there are no mode
ling rules that would guarantee that the activity can be seamlessly decomp
osed to an REA model. Therefore, IDEFO is useful as a tool for commun
ication between users, domain experts, and developers, and is good for
understanding business processes, but its intention is not to link together
the REA components.


<!-- Page 94 -->


### 1.10 REA Value Chain in Detail 81


An application designer could use flow charts for modeling all business
processes of the company. Flow charts, or its UML version called activity
diagrams (UML 2.0 Superstructure Specification, 2005), focus on the ord
er and sequencing of the activities. Creating such model is good for und
erstanding the processes of the enterprise, but it is not at all suitable for
designing a software application that should support many variants of seq
uences. If we could describe the purpose of each process instead of actual
sequences of activities, the software application would increase its ability
to adapt to changes. This way of modeling becomes increasingly important
when the essence of the customer’s business remains the same, but the
technology the customer uses changes. You would like the application
model to be robust against and easily adapt to new patterns of commerce,
such as outsourcing, sub contracting, direct sales, and also patterns unk
nown today.

An application designer could use organization charts. Organization
charts are good for expressing what resources managers and workers will
control, execute, and monitor, but the model should focus rather on the
flow of value in the transition of raw materials to a finished product.

An application designer could use Porter’s value chain (Porter 1980).
Porter’s value chain is a tool and conceptual framework for examining and
diagnosing the competitive advantage of a company. Although very useful
as a modeling technique for business systems, the original purpose of Port
er‘s value chain was not to design software business applications. Porter’s
value chain divides processes of a company into core business processes
that add value to the end products of the enterprise, and support processes
that enable the core processes and add value indirectly. In fact, every proce
ss adds value (otherwise a rational company would not have it), and the
result of analysis should be a complete model expressing how every proce
ss contributes to the complete chain. Sometimes it makes sense to exclude
a process from the value-adding chain, but you should make such decision
as a modeling compromise after the analysis has been performed, rather
than at the beginning of the analysis. Overall, Porter’s value chain, by cons
idering all known processes of the enterprise, is a good starting point in
creating the REA model.


<!-- Page 95 -->


2 Structural Patterns at Policy Level

The previous section, Structural Patterns at Operational Level, described
how to create REA-based application models that model economic exc
hanges that actually occurred.

This section focuses on REA application models that describe the gene
ral rules that govern what events should, could, or should not occur under
certain conditions. The COMMITMENT pattern specifies which events
economic agents agreed upon to occur in the future. The central patterns in
this section are the CONTRACT PATTERN and the SCHEDULE
PATTERN, that bind together commitments and terms, which instantiate
additional commitments in case the agreed commitments have not been
fulfilled. The POLICY PATTERN describes certain kinds of business rules,
the rules or restrictions that the enterprise wants to apply to the economic
events and commitments in which it participates. The GROUP PATTERN
and the TYPE PATTERN introduce the essential infrastructure at the policy
level, as the commitments are often related to types of resources instead of
actual resources, and policies are typically applied to groups of entities ins
tead of actual entities.

The patterns LINKAGE, RESPONSIBILITY and CUSTODY are not the
essential part of the modeling infrastructure, but they are often needed by
business logic as structural elements of the REA application model.

Behavior Customizable Functionality
REA Structure at Policy Level Extended Skeleton
What Could, Should or Should not Happen
CONTRACT SCHEDULE
in trade production
homogeneous || heterogenous | | structure of structure of responsibility
collections collections resources agents for resources

REA Structure at Operational Level Fundamental Skeleton
What Has Happened


<!-- Page 96 -->


84 2 Structural Patterns at Policy Level

### 2.1 Group Pattern

' j Ae | ¥ =
1 Pay BE
, =~ it
ial a ®, |
Often it does not make sense to talk about just actual instances such as “a
copy of Lewis Carroll’s Alice’s Adventures in Wonderland,” or “a copy of
Linda Rising, and Mary Lynn Manns’ Patterns for Introducing New
Ideas” ; we would like to talk about “items on the shelf”
Context
Business rules seldom refer to a specific instance, such as an actual cust
omer or an actual item. For example, Joe’s Pizzeria gives a 20% discount
to the customers living in a specific geographic area. In principle, Joe can
create an individual discount rule for every customer from this area. Howe
ver, if Joe’s Pizzeria has 100 customers from this area, then the business
application would contain 100 rules, and they would all be the same. This
is possible, but impractical. A better solution is to have one rule, and apply
it to the entire set of these customers. However, the REA entities at operat
ional level represent actual resources, events and agents; there is no conc
ept representing sets or collections.
Problem
How do we model heterogeneous collections or sets of REA entities?
Forces
The following forces shape the solution:
e The purpose of the business rules is to give general guidelines that are
applicable to groups of economic resources, events, and agents, rather
than to actual resources, events, and agents.


<!-- Page 97 -->


### 2.1 Group Pattern 85


e The REA model at operational level does not contain any entity that
could naturally represent groups of things that share something in comm
on.

e There are no restrictions on who the members of the group could be.
Members of the same group do not need to have anything in common,
except the fact that they belong to the same group. For example, there
can be a group containing both economic events and resources. In other
words, groups are heterogeneous collections.

Solution

Introduce a group as a structural element of the REA application model.

An REA entity group represents a set of REA entities that have something

in common. The group entity is related to its members by a grouping rela-

tionship. Members of the group can be any entity in the REA model: res
ources, events, agents, commitments, claims, contracts, types, or other
groups.
0..*
grouping
0..*

Fig. 58. Group and grouping
Grouping is a many-to-many relationship. An REA object can be a

member of several groups simultaneously, and a group can have (and usu-

ally has) several members. There can be REA objects that do not belong to
any group, and there can be a group that does not have any members (for
example, there can be no books on the shelf).


<!-- Page 98 -->


86 2 Structural Patterns at Policy Level
apply 0.* apply
0..*
0..*
0.* apply | 9 « 0*
0..* 0. * 0..*
Resource Group — Agent Group
0. 0." grouping 0+ o.* grouping 0+ 0." grouping
grouping grouping grouping
0. 0.* 0.*
Event Agent
Fig. 59. Groups and their relationships to other REA entities

Fig. 59 illustrates some relationships between the group entity and other
REA entities. Policy (see the POLICY PATTERN) can be related by an app
ly relationship to groups of events, agents, and resources. Groups can also
be related to other groups; and they can also be members of other groups.

In the simplest cases, users of business applications maintain links bet
ween members and groups. For example, if a user creates a new catalogue
item, he will also assign this item to the correct VAT (value added tax)
group.

More sophisticated solutions let business applications determine what
groups a member belongs to based on the value of the properties of the
member entity. This functionality can vary from one application to ano
ther, and from one grouping to another. One possible implementation is
CLASSIFICATION PATTERN, described in Part II, Behavioral Patterns.
Another possible implementation is BUDGET (not described in this book);
a budget is a group of economic events or commitments that are expected
to occur in the future.

Examples

The group is an important element in specifying business rules. Groups
can be used to classify resources into tax groups; trading partner groups
can reflect their value to the enterprise; and employees can be grouped acc
ording to their skills. A budget is a group of events or commitments exp
ected to occur in a well-defined time interval in the future. Customers can
be high-volume and low-volume.


<!-- Page 99 -->


### 2.1 Group Pattern 87


msn“ Shopping
Search Shopping "Search ¢
Home > Books & Magazines > Children & Teen Books > Juvenile Fiction > Action & Adventun
Action & Adventure: Action & Adventure Books
Fig. 60. Groups of products in MSN shopping

Fig. 60 illustrates examples of groups at http://shopping.msn.com. A
product can belong to the groups Home, Beauty, Books, Clothing, Comp
uters, Deals, Electronics, Flowers and others. The Home group has subg
roup Books & Magazines, which has a subgroup Children & Teen Books,
which has a subgroup Juvenile Fiction. A specific product can belong to
several groups; for example, Harry Potter and the Half-Blood Prince by
Rowling, J. K. is both a Science Fiction, Fantasy, & Magic and an Action
& Adventure book.


<!-- Page 100 -->


88 2 Structural Patterns at Policy Level

### 2.2 Type Pattern


FB GPS
wie = be
Types are homogeneous groups; all their members conform to certain
definitions, descriptions or blueprints
Context
Product catalogues often contain a description of the resources that a cust
omer can buy, rather than actual resources. When customers place an ord
er, they specify the parameters of the product; when a vendor successf
ully fulfills the order, he delivers an actual product that conforms to the
parameters of the customer’s order.

A similar story can be told for production. A recipe or blueprint contains
the parameters, description, or definition of a product that is produced or a
raw material that is consumed. When the production is successfully comp
leted, products that match the blueprints are produced.

If you design a business application, you often need to create a model
that contains catalogue-like descriptions of resources, events, and agents.
Problem
In the REA application models, there is often need for an entity that holds
the description or definition of a resource, an event, an agent, or another
REA entity. However, the REA model at operational level does not have
an entity that can naturally represent the catalogue-like description, definit
ion, and blueprints.

Forces
The following forces need consideration:


<!-- Page 101 -->


### 2.2 Type Pattern 89


e Catalogue items describe economic resources, but they often do not ref
er to actual, unique items. It is also common that sales order lines refer
to catalogue items specifying features of the resources, rather than
specifying actual instances of goods.

e Business rules seldom refer to an actual instance, such as a physical cust
omer or a physical item. The purpose of the rules is to give general
guidelines that are applicable to certain types of economic resources,
events, and agents, rather than actual resources, events, and agents.

e As similar entities share their features and properties, the model can be
simplified by extracting the shared features and properties and moving
them to another entity, which will be related to the entities that share the
features or properties.

Solution

Introduce an Economic Resource Type, Economic Agent Type, Economic

Event Type, Commitment Type, and Contract Type as structural elements

of the REA application model. They hold the definition or description of

an economic resource, event, agent, commitment and contract, see Fig. 61.

Economic . . .
Economic Economic Commitment
0..1 0..1 0..1 0..1 0.1
specification specification specification specification specification
0.." 0." 0.." 0.." 0."
Resource Event Agent

Fig. 61. REA types and REA entities
Conceptually, every REA entity has an REA type, but an REA applica-

tion model does not need to contain the REA types if there is no need for

it. Conversely, for a given REA type, there is no requirement for there to

be an instance of this type. An REA entity and its type are related by a

specification relationship. Resource types can be related by a reservation

relationship to the commitments. Fig. 62 illustrates typical use of Econ
omic Resource Type.


<!-- Page 102 -->


90 2 Structural Patterns at Policy Level
Economic reservation
Resource Type 0.1 | O.*
\
0..1 \ 0."
specification allocation
0.. ae
Economic reservation
Resource 0..*
Fig. 62. Relationships between REA types
Examples
A seat on a train with the following description “business class, nons
moking, window” is a resource type. The seat with “number 11 in car
number 22 of train IC 129 from Copenhagen to Aarhus on 25 April 2005,
departing at 9:00 hours from Copenhagen” is an implementation of this
type.
«specification»
Fig. 63. Car model as a resource type
A specific car with serial number e.g. VF32CBFZE40227290 is an econ
omic resource; its resource type is a definition or specification of this res
ource, such as Ford Focus Trend, see Fig. 63.
A labor type (see Fig. 64) is a qualification or set of standard skills req
uired for a specific job. A labor instance is the qualification and the set of


<!-- Page 103 -->


### 2.2 Type Pattern 91

skills of a physical person. A work of a certified public accountant is a lab
or type. The work of accountant “Jette Friisdahl on 8 May 2005 from
8:30 a.m. to 11:30 a.m.” is an implementation of this type.

«resource type»
Labor Type
«specification»
«resource»
Labor
employee's specific skills
when performed
Fig. 64. Labor type and actual labor
Resulting Context
In many business applications, the list of resource types should not be enc
oded into a business application; that is, the users of a business applicat
ion should be able to add and remove resource types at runtime. For exa
mple, Navision Financials has an entity Item, representing all tangible
resource types, and an entity Work, representing resource types similar to
services.

Types enable users to add more business knowledge into a business app
lication, something that has both benefits and drawbacks. The drawback
is that the business knowledge in the software system needs maintenance.
The benefit is that a software application that is aware of the business
knowledge can more efficiently guide and help its users.

Sometimes we come across a situation in which a resource changes its
type during its lifetime. The change of type is usually the result of a conv
ersion process; therefore, we suggest modeling the type change as the
consumption the resource of the old type and the production of a resource
of the new type.


<!-- Page 104 -->


92 2 Structural Patterns at Policy Level


### 2.3 Difference Between Types and Groups


All tomatoes are of the same type, but belong to different groups

Types and groups both represent sets of objects, and in this sense types and
groups are similar. However, information captured by a group and a type is
different.

Groups are heterogeneous collections; they can contain members of diff
erent types, although their members might share some characteristics, for
example, we might have a group of tomatoes that are all red.

Types are homogeneous collections; all members have the same charact
eristics defined by the type. A type is a special kind of group. It is a group
that defines concepts and characteristics that apply to all current and future
embodiments of the type.

The decision whether a specific collection should be modeled as a type
or group often depends on its intended use in the REA model. In REA,
groups are typically used to specify policies; policies are typically applied
to groups. Types are typically used to specify reserved resources (in the
cases the actual resources cannot be specified at the reservation time);
commitments via reservation relationships are typically related to types.

An REA entity can be a member of several groups simultaneously, and
can change its group and be removed from a group, usually as a result of
the changing of values of its attributes, properties, methods or other chara
cteristics. When an object changes its group, it does not change its definit
ion, because it is defined by its type.

Groups, but seldom types, often contain properties for aggregated or stat
istical values derived from the properties of their members. For example,
the group of tomatoes in a basket might contain a property for the total
number, total weight, and average weight of the tomatoes in the basket.


<!-- Page 105 -->


### 2.4 Commitment Pattern 93


### 2.4 Commitment Pattern


Sales Order
Enterprise: Joe’s Pizzeria Date: 11 February 2005
Customer: Addy
Number Amount Item Price

Sales order lines are not economic events; they are promises of economic

events

Context

Most economic events do not occur unexpectedly. Economic events are

usually scheduled or agreed upon beforehand by economic agents. For ex-

ample, a sales order line is a promise to sell goods to a customer; the total
price is a customers’ promise to pay for the goods, and the seller’s promise
to accept the payment.

Problem

How do we model promises of future economic events?

Forces

Solving this problem requires the resolution of the following forces:

e Application designers would like to have a mechanism in the application
model specifying details about the promises of economic events. Econ
omic events cannot be used for this, because economic events specify
actual increments and decrements of resources, while promises result
only in reservations of resources.

e There might be (and usually is) a difference between plans and what act
ually happens. The users of a business application would like to know


<!-- Page 106 -->


94 2 Structural Patterns at Policy Level
whether the economic events occurred as they were promised, and inf
ormed about eventual differences.

e If an enterprise promises to give its own resources to its trading partn
ers, users of business application would, most likely, like to know,
what resources to expect in return. Conversely, if an enterprise expects
to receive some resources, the users of business application would like
to know what resources its trading partners expect.

e If an enterprise schedules to the production of resources, users of a
business application would like to know what resources it would require
to use or consume. Conversely, if an enterprise plans to use or consume
resources, the users of a business application would like to know what
resources will be produced from them.

e The users of a business application would like to know who should be
responsible for the received or produced resources, and who should be
responsible for the resources used or consumed during the production or
given to other economic agents.

e For each promised exchange, the users of a business application would
like to know the trading partners to whom the resources should be transf
erred, and from whom they should be received.

Solution

Model the promise of the economic event as a commitment entity. A com-

mitment in exchange processes represents obligations of economic agents

to provide or receive rights to economic resources. A commitment in conv
ersion processes represents scheduled usage, consumption, or production
of economic resources.

Each commitment is related to an economic event by a fulfillment relat
ionship, representing the fact that commitments are fulfilled in the future
by one or more economic events executed by the participating economic
agents, see Fig. 65. Commitments have usually properties for the Schedu
led Date or period of the economic event, and the Scheduled Value of the
event.

The Scheduled Value does not need to be expressed as an actual numb
er, but, for example, as a rule. For example, the price of a service can be
determined according to actual costs.


<!-- Page 107 -->


### 2.4 Commitment Pattern 95


Scheduled value
Scheduled date of event

0..*

fulfillment
Actual value
Actual date of event
Fig. 65. Commitment and economic event

Each promised exchange and conversion consists of at least two comm
itments: increment commitments, which are expected to increase the
value of economic resources, and are fulfilled by increment economic
events, and their related decrement commitments, which are expected to
decrease the value of economic resources, and are fulfilled by decrement
economic events. The relationship between increment and decrement
commitments identifies which resources are promised to be exchanged or
converted to which others, and is called exchange reciprocity or convers
ion reciprocity.

Fig. 66 illustrates relationships between commitments, economic events,
economic agents, and economic resources in exchange processes. Fig. 67
illustrates the same relationships in conversion processes.

Provide and Receive

Commitments are related by the provide and receive relationships to the
economic agents that are scheduled to participate in economic events, and
they consequently determine who should have rights to or the control over
economic resources. The provide and receive are one-to-many relations
hips. One economic agent can participate in zero or more commitments;
an economic commitment must have exactly one committed provider and
exactly one committed recipient economic agent.


<!-- Page 108 -->


96 2 Structural Patterns at Policy Level
0..* 0..*
intended provide [|X ; ‘Agent re .
and receive | receive : provide
nn a 0.. 0.. outflow
0.* | provide receive reservation
0..1 inflow 0..* 0.. 0..*
= - reservation exchange
Rosoures 0..1 0..*| Increment reciprocity Decrement |o0..*
Type Commitment 1% 1..* | Commitment
0..* 0.*
0..* . . . 0..* 0..* outflow
specification inflow fulfillment i
0.* reservation fulfillment 1 1 reservation
Economic 1 Increment ried Decrement
Resource inflow Economic Economic
Event Event
1 1%
Fig. 66. Relationships of commitments in exchange process
consume reservatio'

0..* 0..* use|
intended [, Economic reservation
provide and |-~----receive Agent provide
receive a 0.. 0..*

0.* | provide receive
0..1 0..1 produce 0..* ; 0. 0..*
E , reservation conversion 0..*
Resources 0..1 0..*| Increment reciprocity Decrement 0..*
Type Commitment | 4.* 4..* | Commitment
yp 0." 0.7
o.- pecification produce 0." 0." a
i fulfillment
0." reservation fHfillment , 1
1 conversion
Resourses promucs Event 1." 1." Event
1 1
4 1
1] IH use -
consume
use reservation
consume reservatio!
Fig. 67. Relationships of commitments in conversion processes


<!-- Page 109 -->


### 2.4 Commitment Pattern 97

Exchange and Conversion Reciprocity
The exchange reciprocity relationship between the increment and decrem
ent commitments identifies in the model which resources are promised to
be exchanged for which others. Likewise, the conversion reciprocity ident
ifies which resources are promised to be used or consumed in order to
produce others.

The commitments paired by the reciprocity relationship do not need to
be instantiated (created at runtime) at the same time. For example, the ins
urance process illustrated in the Modeling Handbook contains an example
of an increment commitment (insurance payment) that is instantiated only
under certain conditions specified by the insurance contract. Another exa
mple is a commitment to buy shares of a company, paired with a reciproc
al commitment of dividend payments. The two commitments are instantia
ted at different times.

Fulfillment
The purpose of the fulfillment relationship is to validate whether the econ
omic events fulfill their commitments.

This can often be done automatically. For example, the RECONCILIA
TION PATTERN (see Behavioral Patterns) can validate that quantity on
the sales order line (i.e., the value of the commitment) is the same as the
sum of the shipped quantities (values of the economic events).

Sometimes, a human decision is needed to determine whether the econ
omic events fulfill their commitments. For example, if a payment comm
itment was fulfilled by payment in different currency, due to variable exc
hange rates the monetary value of the commitment can differ from the
monetary value of the economic event. In such cases, a human decision
might be needed to judge whether the difference is sufficiently small to
consider the commitment fulfilled.

The fulfillment relationship is a many-to-many relationship between the
economic commitment and the economic event. One economic commitm
ent can be fulfilled by several economic events, just as one shipment
commitment can be fulfilled by partial shipments, and one economic event
can fulfill several economic commitments, just as several installments can
be paid once.

Reservation

In order to specify what resources will be needed or are expected by future
economic events, each economic commitment is related to an economic res
ource or a resource type by a reservation relationship. For example, a


<!-- Page 110 -->


98 2 Structural Patterns at Policy Level

sales order line is a decrement commitment to ship goods; the sales order
line is related by the outflow reservation relationship to the goods or the
goods type.

When sales persons accept customer orders, they want a business softw
are application to check whether there are products available when the
order is due, and to make sure that these products will be available to the
customers during the economic event related to the economic commitment.

The reservation relationship between the resource and commitment
represents the features of the resource and rights associated with the res
ource that will be changed or transferred by a future economic event, see
Fig. 68.

«commitment» :
= 0..*
reservation

fullfillment

1" 0..*
new memes actual use of the LS
Appartment 1 \ 0..* Rental ~| arom

Fig. 68. Reservation

The commitment can be related to either a resource type or a resource.
For example, a sales order for a new car contains a commitment to deliver
a car of a certain model, (i.e., resource type); a sales order for a used car
contains a commitment to deliver a physical car (i.e., resource). The reserv
ation of a hotel room contains a commitment to provide a hotel room with
certain characteristics, such as of a certain size and with certain number of
beds (i.e., resource type); but sometimes a guest might require a specific
room, in which case the commitment is related to an (actual) resource.

If economic commitment is related to resource type, at some point in the
future, but always before the economic events starts, the commitment must
also be related to an actual resource that conforms to the reserved resource
type, see Fig. 69.


<!-- Page 111 -->


### 2.4 Commitment Pattern 99

Resource reservation Decrement
Type i Commitment
allocation
specification RY fullfillment
reservation
R outflow Decrement
esource Event
Fig. 69. A commitment must eventually be related to an actual resource
The time of allocation varies. For example, in the hospitality business,
the reservation is related to a room specification until the day of the guest’s
arrival. The morning of the day of arrival, the receptionist assigns (a hum
an assisted, and not an automated task) a room numbers for each reservat
ion that starts that day. In the airline business, physical seats are assigned
at the time of reservation, with the possibility of replanning. In theatres
and cinemas, the tickets are assigned at the time of reservation. The algor
ithm first reserves the best seats within a certain price group, such as
those in the middle of the theatre, and later reserves the seats closest to the
best places.
We call the commitment fully specified if it is related by the reservation
relationship to an (actual) resource.
Domain Rules
The following domain rules apply to any REA application model. As
commitments are a mirror image of the economic events at the policy
level, the domain rules are similar to the rules for the REA model at operat
ional level, with one addition: commitments must be fulfilled by econ
omic events. These rules can be used to ensure consistency of REA applic
ation models.
Each commitment must be related to a resource, and might (but does
not have to) also be related to a resource type.
Each commitment must be related by provide and receive relations
hips to economic agents.


<!-- Page 112 -->


100 2 Structural Patterns at Policy Level
Each increment commitment must be related by a exchange or conv
ersion reciprocity relationship to a decrement commitment, and
vice versa.
Each increment commitment must be related by a fulfillment relat
ionship to at least one increment economic event, and each decrem
ent commitment must be related to at least one decrement econ
omic event.
A commitment that is part of a conversion must be related to the
economic event of a conversion process; likewise, a commitment
that is part of an exchange must be related to an economic event of
an exchange process.
Resulting Context
The reciprocity relationship often has additional functionality that relates
together the values of the increment and decrement commitments. For exa
mple, in economic exchanges, the reciprocity can calculate the total price
(value of the outflow commitments) based on the line item prices (value of
the inflow commitments). The reciprocity might also validate that the cost
(value of the outflow commitments) is lower than the price (value of the
inflow commitments). The functionality of the reciprocity relationship can
vary in different implementations; but the fundamental point is that the inc
rement and decrement commitments are related.


<!-- Page 113 -->


### 2.5 Contract Pattern 101


### 2.5 Contract Pattern


| oss ,
a we x ae

Contracts are statements of intent that regulate the behavior among or-

ganizations and individuals. Clauses of a good contract define what should

happen in the cases of cancellation and violation of the commitments

Context

Commitments represent the optimistic path of an exchange. For example, a

sales order contains commitments to deliver goods and commitments to

pay. However, sometimes goods are not delivered as expected and paym
ents arrive late. Partners usually also agree upon what should happen if
the initial commitments are unfulfilled.

Problem

How do we specify in the REA model what should happen if the commit-

ments are unfulfilled?

Forces

We need to balance the following forces:

e Commitments specify what economic events should occur. However, in
the case in which they do not occur as they should, economic agents
usually agree upon what should happen next. The rules specifying what
should happen next can be very complex, and keeping track of what
should happen, and when, can be cumbersome. Therefore, application
developers would like this information present in the business applica-


<!-- Page 114 -->


102 2 Structural Patterns at Policy Level
tion, so that these rules and actions can be monitored and triggered
automatically.

e There are usually several inflow commitments paired through exchange
reciprocity with several outflow commitments. These commitments are
often considered a unit. Sometimes, it does not make sense to fulfill
only some commitments and not to fulfill others, but sometimes this is
acceptable. Application designers would like some entity to contain
such rules.

e Intended recipients or providers of the resources might be different econ
omic agents than the agents that agree about the exchange.

Solution

If a commitment is unfulfilled, the terms of a contract specify additional

commitments.

A Contract is an entity in the REA application model containing increm
ent and decrement commitments that promise an exchange of economic
resources between economic agents, and terms. Commitments were disc
ussed in the previous pattern. Terms are potential commitments that are
instantiated if certain conditions are met. These conditions can be various,
such as a commitment not being fulfilled, or a resource being at a certain
location. For example, economic agents can agree upon penalties if the
commitments are unfulfilled. If the commitments are unfulfilled, the cont
ract will instantiate a new commitment to pay a penalty. The terms and
commitments are the clauses of the contract

Every contract must be related to two or more economic agents by a
party relationship. These agents do not necessarily have to be the provider
and recipient of economic resources. The economic agents that are parties
in a contract can be different from the economic agents related to the
commitments within the same contract, and different from the agents part
icipating in the economic events which fulfill these commitments. For exa
mple, a flower shop can deliver flowers to a different person than the one
who placed the order, and the flowers will be paid for by a third person,
different from the persons who placed the order and received the flowers.


<!-- Page 115 -->


### 2.5 Contract Pattern 103

clause clause
0..* | 0..* \ clause 0." clause i 0..* | 0..*
create 1* 1" create
. exchange
Commitment 1. 1. Commitment
; party ; 0.* 0..*
0..* 0..* provide provide receive
receive receive 1 1 receive
oe
1 1
Fig. 70. Contract, commitments and terms
Offer and Quote have the same structure as contracts that have not been
accepted by all parties in a contract. Economic agents negotiate the content
of the commitments and terms, and when they agree upon commitments
and terms, the quote or offer becomes a contract that binds the agents that
are parties in the contract. There is usually certain period of negotiations
and draft versions from when the offers and quotes are created and until
the contracts are accepted by both contracting parties.
Examples
Examples of contracts are sales orders, purchase orders, contracts for prov
iding various services, and employment contracts. We illustrate several
examples of contracts in the Part Four of this book, Modeling Handbook.
Fig. 71 illustrates a business document for a simple sales order without
delivery and payment terms. The REA application model for this sales ord
er is illustrated in Fig. 72. The Sales Order contains two Sales Lines
specifying the goods; the sales line entity corresponds to the line item in
Fig. 72. The Payment Line specifies the price (i.e. expected amount of rec
eived cash); the entity Payment Line corresponds to the Total line in
Fig. 72.


<!-- Page 116 -->


104 2 Structural Patterns at Policy Level
Sales Order
Enterprise: Joe’s Pizzeria Date: 11 May, 9:15
Customer: Addy
Number Item Quantity Delivery Time
6128 Pizza Margherita 2 units 11 May, 18:00
8694 Cola 0.51 1 unit 11 May, 18:00
Total 21,00USD 11 May, 18:30
Fig. 71. A sales order is an example of a contract
«party» 0." «contract» «party»

«agent» 1 buyer 0. Sales Order 0..* seller 4
Customer Enterprise
«clause» 1 1 «clause»

«decrement «increment
«reservation» Cueetne. «reciprocity» Payment Line «reservation»
0. DeliveryTime 0. 0. PaymentTime 0.
1 Quantity Amount 1
Ew
Item «resource»
Cash

Name

Number
Fig. 72. The REA application model of a sales order

Fig. 73 illustrates an instance of this Sales Order (an actual sales order
that conforms to the application model from Fig. 72; the data of this sales
order corresponds to that in the example in Fig. 71.

Note that the REA model does not specify how to calculate Total, e.g.
how the amount of 21 USD is related to two units of pizza and 0,51 of
Cola. The calculation rules may range from a simple sum of the unit prices
of the pizza and cola, to the complex rules taking into the account the ident
ity of the customer, date, time, and volume of the sale. The fact that price
calculation vary from one software application to another, is the reason
why the price calculation is not part of REA. REA formulates the fundam
ental principles common to all business applications. Part II of this book,
Behavioral Patterns, shows how to extend the REA skeleton by application
specific functionality.


<!-- Page 117 -->


### 2.5 Contract Pattern 105

«party» buyer «contract» «party» seller
Sales Order
«economic agent» | «economic agent»
Customer: Addy “cans Enteprise: Joe's Pizzeria
«clause» «clause»
«decrement
«reservation» commitment» «increment
Line1: Sales Line commitment»
DeliveryTime: 18:00 «reciprocity» Total: Payment Line
<S PaymentTime: 18:30
Amount: 21USD
«resource type»
|
«reservation»
Number: 6128 sdecrement
Line 2: Sales Line Cash
DeliveryTime: 18:00
Name: Cola 0,51
Number: 8694
Fig. 73. An instance of the REA application model of a sales order

Fig. 74 illustrates a more complicated example of a sales order with
shipment and payment terms. For example, Joe’ Pizzeria and Addy agree
that Joe’s Pizzeria will sell five units of Pizza Margherita to Addy on
Tuesday, and Addy will pay for them on Friday. If Joe’s Pizzeria does not
ship on Tuesday, Joe’s Pizzeria pays a 20 USD penalty to Addy on Friday.
If Addy does not pay on Friday, he pays a 30 USD penalty to Joe’s Pizzer
ia the following Monday. The informally sketched properties of the terms
and commitments can be implemented as DUE DATE PATTERN and
VALUE PATTERN; see Part II, Behavioral Patterns.

If Joe’s Pizzeria does not deliver 5 units of Pizza Margherita on Tuesd
ay, the contract instantiates the penalty, and the model between Thursday
and Friday looks as in Fig. 75.

After the Penalty Payment commitment has been instantiated, all comm
itments still need to be fulfilled by economic events. According to this
contract, the Joe’s Pizzeria still has to ship and the agent Addy has to pay,
even in the case in which Joe’s Pizzeria does not ship at all. A better cont
ract might specify that the payment should occur within a certain time per
iod from the shipment.


<!-- Page 118 -->


106 2 Structural Patterns at Policy Level
«economic agent» «economic agent»
Customer: Addy | buyer seller | Enteprise: Joe’s Pizzeria
«party» «party»
«decrement term» «merernent om
Penalty Payment Penalty Receipt
«clause» ;

Sale not fulfilled on «contract» «clause» gash Receipt ‘iy

Tuesda Sales Order ultilied on Friday

DateTime: Friday DateTime: Following

Quantity: 20 USD DateTime: Monday Monday

Quantity: 30 USD
«clause» «Clause»
«decrement «increment
. commitment» commitment»
«reservation» Line1: Sales Line Total: Payment Line
«reciprocity»
DateTime: Tuesday DateTime: Friday
Quantity: 5 units Quantity: 35 USD
«reservation»
«resource type»
Name: Pizza Margherita Cash
Number: 6128 Account 750-2922
Fig. 74. Simple contract with shipment and payment terms
Resulting Context
The precise specification of commercial contracts is a subject of intensive
research. Simon Peyton-Jones, Jean-Marc Eber, and Julian Seward have
developed a functional language for financial contracts; this language does
not have an REA concept of reciprocity (Peyton-Jones, Eber 2003). A lang
uage for REA-compatible contracts is being developed by Fritz Henglein
and his students (Henglein 2005).

There are also higher level agreements between economic agents that
regulate the behavior of the individual contracts. Agreements differ from
contracts in that they do not contain commitments, but only conditional
clauses, and they are hierarchical in nature. Agreements are sketched in
Fig. 76.

An example of an agreement is a service level agreement for mainten
ance of equipment, which specifies, for example, that the enterprise may
place maintenance orders (i.e. contracts) under specific conditions, and rec
eive discounts for specific services.


<!-- Page 119 -->


### 2.5 Contract Pattern 107

«economic agent» «economic agent»
Customer: Add buyer seller | Enteprise: Joe’s Pizzeria
«decrement term» «party» «Party» «increment terms
Penalty Receipt
Penalty Payment
«clause» f
Sale not fulfilled on «contract» «clause» rash Receipt ‘day
Tuesda Sales Order y
DateTime: Friday DateTime: Following
Quantity: 20 USD DateTime: Monday Monday
Quantity: 30 USD
H «clause» «Clause»
«decrement «increment
fy commitment» commitment»
«reserva ala Line1: Sales Line Total: Payment Line
\ DateTime: Tuesday «reciprocity» DateTime: Friday
\. | Quantity: 5 units cS Quantity: 35 USD
«resource type» ~SY «reservation»
Item x
«decrement
Name: Pizza Margherita commitment» «resource»
Number: 6128 :Penalty Payment Cash
reservation Account 750-2922
“ , Quantity: 20 USD
«resource»
Cash
Account 390-8969
Fig. 75. Simple contract after one of the terms’ conditions has been met

### 0.4 governing

agreement
clause
' ~
f 0..*
0 { either or } governing |0.-1
“ y agreement 0."
clause \
Term :
0." 1 party
0..*
0..* 0..* party
2..*
receive 1
provide Economic Agent -—~,
1
Fig. 76. Agreement and contract


<!-- Page 120 -->


108 2 Structural Patterns at Policy Level

### 2.6 Schedule Pattern

Sf.
wer
at o A
= &

Schedule is a series of things to be done or of events to occur at or during

a particular time or period

Context

Production processes usually do not occur spontaneously; a rational com-

pany schedules the production and usage of its resources that should take

place in the future. However, production sometimes does not occur as
planned because of unexpected circumstances. A rational company would
like to mitigate risks and determine additional factors that should occur if
the originally planned operation does not occur as expected. Making a plan

is a way to minimize the risks of missing some resources in the middle of a

production. The purpose of the plan is to make sure that for all processes

the needed resources are identified, as well as when they will be needed.

Problem

How do we specify conversion processes that should occur in the future?

Forces

The following forces influence the solution:

e If use, consume, and produce economic events do not occur as commitm
ents specify, the enterprise would like to have an alternative plan to
mitigate the consequences. Application developers would like this inf
ormation present in the business application.


<!-- Page 121 -->


### 2.6 Schedule Pattern 109


e A conversion process usually consists of several use, consume, and prod
uce economic events that have various, often complex dependencies on
each other. If some of these events do not occur as committed, the mitig
ation plan depends on a combination of the values of the economic
events. The application model should contain an entity containing such
dependencies.

e The economic agents that are responsible for the overall conversion
process can be different from the agents that control the economic res
ources.

Solution
A schedule is a collection of increment and decrement commitments in

conversion processes and mitigation plans. Mitigation plans instantiate add
itional commitments under certain conditions, typically if some of the
original commitments are unfulfilled, see Fig. 77. Unlike invoking penalt
ies in the contracts, instantiating commitments from mitigation plans is
usually not an automated task, and it requires the assistance of the users of
business applications.

A schedule is related by a party relationship to the economic agents that
are responsible for the schedule. The agents that are related to the schedule
can be different from the agents that are related to the commitments. There
are usually two agents related to the schedule. One of the agents sets the
requirements of what should be done (representing a client in the planning
process), and another agent is responsible for the actual conversion, (repres
enting the supplier in the planning process).

clause

0..* 1 party
Lo ° .
create 0.1 0.41 4) 4) 4] 4
create clause NO clause

| NS
1 ~s. 1" |

; ; . 0..* receive
Increment reciprocity Decrement F

Commitment |, « 1.*| Commitment 0..* provide
0.* 0.* receive
provide
Fig. 77. Schedule


<!-- Page 122 -->


110 2 Structural Patterns at Policy Level
Example
The example in Fig. 78 illustrates a simple schedule of a project Produce
Pizza, assigned to Tom, Susie, and Mike. Project Produce Pizza is an inc
rement commitment, and the consumption of the labor of Tom, Susie, and
Mike are decrement commitments.
oust
ID Task Resources uration BOORECoeEoEo
fpr | |? | rr
epee fe [= Tm
Po Se |? |__|
cleo fe = |) — |
Fig. 78. A simple schedule
The REA application model corresponding to the diagram in Fig. 78 is
illustrated in Fig. 79. The schedule Project Schedule has an increment
commitment Project, which reserves (expects) the economic resource
Pizza. The decrement commitment Task reserves consumption of the econ
omic resource Labor. The properties start, finish, and duration can be imp
lemented as DUE DATE PATTERN; see Part II, Behavioral Patterns.
«clause» «schedule» «clause»
0..1. | Project Schedule | 0.1
«decrement «increment
i i
«consume Task «reciprocity» Project «produce
reservation» 0,.* | duration 0..* 0..* | duration reservation»
start start 0
end end “
1 1
«economic resource» «economic resource»
Labor Pizza
Fig. 79. REA application model for simple schedule
Fig. 80 illustrates an instance of the REA application model from
Fig. 80 that corresponds to the example in Fig. 78.


<!-- Page 123 -->


### 2.6 Schedule Pattern 111

«clause» «schedule» «clause»
:Project Schedule
«clause»
«consume «clause»
reservation» «decrement
commitment»
Dough: Task
Duration: 1 hour \Y oe
«resource» Start: 8.00 «reciprocity» | increment commitment»
Tom: Labor End: 9.00 Produce Pizza: Project
» Duration: 10 hours
ssorvation «decrement Start: 8:00
reservation» commitment» End: 18:00
Toppings: Task
_ «produce
«resource» Duration: 3 hours reservation»
Sussie: Labo Start: 8:00
suSSieSner End: 11:00
«resource»
«consume :Pizza
reservation» «decrement
commitment»
Baking: Task
Duration: 4 hours
«resource» Start: 14:00
Mike: Labor End: 18:00
Fig. 80. An instance of the REA application model of a schedule
There are many examples where the detailed schedule means success or
failure for the whole company. In just-in-time production, the resources
are delivered exactly when they are needed. Delivery too early would
mean a need for storage and late delivery can stall the production.


<!-- Page 124 -->


112 2 Structural Patterns at Policy Level

### 2.7 Policy Pattern

ALCOHOL FREE ZONE
9pm - Jam
1 Nov - 30 Mar
A policy is a rule of practice or procedure to guide decisions and actions
Context
Not everything is allowed; law, system, tradition, culture, and internal
company rules constrain the economic exchanges or conversions that are
possible or desirable in any situation. For example, rules might specify
what qualification of employees is needed to perform certain operations, or
what kind of equipment is needed to transport hazardous materials.

The rules and constraints can be specified in media other than a software
application, for example, in a policy handbook that users of business applic
ations have to study. However, it would be more efficient and useful if a
software application could be aware of the rules and constraints, and help
users act upon them. For example, if a user tries to register or orchestrate
an event that does not conform to the rules, the business application could
inform the user of the rule violation, advise him on what to do instead, and
prohibit him from committing or executing an illegal exchange or convers
ion.

The REA application model specifies the economic events, agents, and
resources applicable to a certain line of business. Using the core REA app
lication model, users of business applications can plan and register any
kind of economic event that is part of the application model. The core
REA model alone does not have a placeholder for rules governing what
types of economic events are allowed or not allowed in certain situations.
Problem
How do you make the business application aware of the fact that some
economic events are not allowable or desirable in certain circumstances,
and even prevent users from doing something illegal?


<!-- Page 125 -->


### 2.7 Policy Pattern 113


Forces

We need to balance the following forces when modeling such rules:

e The users of business applications, without the help of an application
designer, should themselves be able to create and modify the rules about
the allowability of economic events.

e Business rules are not localizable into a single entity because they repres
ent constraints that affect several entities. However, these rules must be
part of some entity in the model.

e Although constraints are not localizable into a single entity, their imp
lementation should not be scattered across entities in the application.
There should be a single place in the model to hold the rules. It should
be easy to find and document the rules in the system. Likewise, if an ent
ity is affected by some rule, it would be nice to easily identify all rules
that affect this entity. For example, it should be easy to determine what
rules apply to a given customer group.

e As the software application needs to interpret the rules, they should be
represented in the language at the same level of abstraction as the softw
are application. If the application model is an object-oriented model,
then rules should also be represented in terms of objects and relations
hips, and not, for example, as free text. If the model is represented in a
domain-specific language determined by a framework, the rules should
also be represented in the domain specific language, and not, for examp
le, as code in a general purpose language.

e The software application with rules should be open to extensions. For
example, existing rules on customers should not be affected by adding a
new customer group.

e If the rules change, the software application should be able to keep the
old version of the rules, and also to execute the business logic according
to the old rules. For example, there is sometimes a need to register econ
omic events that occurred before the rules changed; such a registration
should be processed according to the previous version of the rules.

Solution

The policy entity encapsulates constraints on the economic exchanges and

conversions. The policy is related to (can be applied to) a group; see

Fig. 81.


<!-- Page 126 -->


114 2 Structural Patterns at Policy Level
apply
0.* 1
Fig. 81. Policy

An example of the REA application model with a Sale Policy is Fig. 82.
The Sale Policy can be applied to Item Group, Event Group, and Customer
Group. This policy can specify that, for all Events related to specific Items
and to specific Customers, certain rules apply. An instance of this policy is
given in Fig. 83.

0..* «policy»
Sale Policy 0..*
«apply» 0." «apply»
«apply»
0..* 0." 0.*
«group» «group» «group»
Item Group Event Group Customer Group
Fig. 82. Policy in the REA application model

Policies should be related to the entities at the policy level, i.e., to the
groups or types, rather than to the entities at the operational level. Policies
related to the groups or types have more explanation power than policies
related to the actual entities, and allow for reasoning about the policy. For
example, if Addy is a customer of Joe’s Pizzeria, which introduces a policy
We do not sell to Addy, this policy includes no explanation. However, if
the enterprise introduces a policy We do not sell to people who have misb
ehaved several times, and Addy belongs to this group, this policy explains
the reason.

Sometimes it might seem that there is a specific policy affecting only a
specific resource, event, or agent. In these cases, the solution in Fig. 81
leads to creating a group with only one member. Although this might be
considered an unnecessary complication, this model forces an application
developer to generalize the policies, and other entities that belong to this
group are typically discovered later.


<!-- Page 127 -->


### 2.7 Policy Pattern 115

Examples
Consider the policy specifying that an enterprise is not allowed to supply
tobacco products to minors. The Supply Policy “Tobacco to Minors” is app
lied to the groups “Tobacco Products,” “Supply,” and “Minors.” Fig. 84
illustrates that if a user of the business system attempts to register an ins
tance of the Sale economic event with “PM Box” as an Item and “Addy “
as a Customer (illustrated by dashed lines), the policy would be enforced;
thus, the sale would not be allowed.

What would really happen in the business application depends on the
implementation of the policy. The system response could range from notif
ying the user of the business system about the violation of the policy (by
raising an information event) to preventing him from registering the event.

«apply» «policy» «apply»
Tobacco to Minors:
Supply Policy
«apply»
«group»
. «group» «group»

iemGo

«grouping» «grouping» «grouping»

«resource» «outflow» «decrement event» «receive» «economic agent»

PM Box: Item ee : Sale a Addy: Customer

’s Pizzeria: Enterpri

Fig. 83. A policy

There are policies applicable only during certain time intervals. For exa
mple, the Sunday Rule policy specifies that the Joe’s Pizzeria does not
sell alcoholic beverages on Sundays. The REA application model in
Fig. 84 contains a group Period of Sale, representing a group of moments
in time. The Period of Sale has the value “Sunday,” and Item Group, has
the value “Alcoholic Beverages,” and they are related to the Sale Policy
entity. If Joe’s Pizzeria attempts to sell an item that belongs to the group
“Alcoholic Beverages” and the time of sale belongs to “Sunday,” the poli
cy would be enforced. Please notice that the Sunday Rule policy is not rel
ated to the Customer Group, which means that it applies to all customers.


<!-- Page 128 -->


116 2 Structural Patterns at Policy Level

This example also illustrates that if a policy becomes obsolete, the users
of the business application can easily restrict its validity in time, rather
than deleting the policy. As the economic events are always registered aft
er they have occurred, this practice enables the users to enforce the policy
on the events that occurred when the policy was still in force, although the
events have been registered after the policy became obsolete. Of course, in
such cases it is not practical for the business application to prevent users
from registering the events that violate the policy, as they already occ
urred, but the business application might notify the users that the policy
has been violated.

«apply» «policy»
Sunday Rule: Sale Policy
«apply»
«group» «group» «group»
Alcoholic Beverages: Sunday: Minors:
Item Group Period of Sale Customer Group
«grouping» «grouping» «grouping»
«resource» __ outflows «receive» «economic agent»
Akvavit 0,7: Item _ Addy: Customer
«provide» «economic agent»
Enterprise
Fig. 84. A time-limited policy

Another example of a policy is “A junior bookkeeper cannot approve a
payment over $50,000.” This policy would be related to the groups “Junior
Bookkeeper,” “Payment,” and “Over $50.000”.

The functionality of the policy entity can be implemented in various
ways. One possible implementation is a behavioral pattern called MATRIX
RULE (not included in this book, but sketched below), which can be used
to implement policies. The name “matrix rule” comes from a representat
ion of this rule in the form of matrix, in which the columns represent the
groups and the rows represent the different policies that apply to these
groups. The matrix representation of the policy is illustrated in Table 1.


<!-- Page 129 -->


### 2.7 Policy Pattern 117

Table 1. A matrix representation of the matrix rule
Period of | Resource Event Event Agent
Sale Group Group Group Group Result
Value Value Value Value Value
All Tobacco All Supply Minor Not allowed
All All Over Payment Junior Not allowed
$50.000 Bookk
eeper
Sunday Alcoholic All All All Not allowed
beverages
Resulting Context
This pattern expresses rules in the form of relations, instead of code.
Therefore, users of business applications can add more rules, and modify
and remove existing ones without modifying code.

It is easy to determine which policies apply to a specific entity by identif
ying the groups of which this entity is member, and traversing the relat
ionships between the groups and policies.

The architecture of the business application must support adding new
groups and policies and relating them at run time. If the actual implement
ation of a business application does not support it, or if the business applic
ation has only one or very few policies, and they are not going to be
changed and no new ones added, then this pattern does not apply. The
models described in this book allow for adding new groups and policies,
but not new group types and policy types. For example, if the application
model contains a policy called Sale Policy, users of the business applicat
ion can add, modify, and remove various Sale Policies. However, the use
rs of business applications cannot add a policy of type Purchase Policy
because it would require modifying the application model. We made that
choice because the model with the entities Customers, Vendors, etc. is easi
er to explain than a more general model that would allow for dynamic
modifications.

Users of a business application need to identify the right groups; otherw
ise, they cannot specify the policies. The information necessary to evalua
te the policy must be in the system. For example, if there is a policy not to
supply alcoholic beverages to people under a certain age, the age of the
buyer must be in the system.

We need to consider the intended results of the individual policies, and
to establish the infrastructure that supports these results. The results of
policies always prohibit some events, but they can be implemented with


<!-- Page 130 -->


118 2 Structural Patterns at Policy Level
varying levels of enforcement, from notifying the user of the application to
preventing him from executing the prohibited action.

A policy entity does not have to be related to groups of commitments, as
information about whether the commitments conform to the policy can be
derived from the policies applied to groups of economic events.


<!-- Page 131 -->


### 2.8 Linkage Pattern 119


### 2.8 Linkage Pattern


I} . BR wen

If you build a house, what are you going to build it out of?

Context

Some economic resources, such as gasoline, are homogeneous units, but

some consist of parts. Parts of the economic resources are also often eco-

nomic resources. A bicycle consists of a frame and wheels, and a wheel
consists of a tire, hub, a rim, and spokes. For scheduling a conversion
process, it is useful to specify the parts of an economic resource consists.

Problem

How do we capture in the REA model information about the structure of

the economic resources?

Forces

Three forces drive the solution to this problem:

e Many resources can be considered as consisting of parts. However, inc
luding a new “part” entity in the REA modeling framework is not a
good solution, because parts are economic resources as well.

e There can be multiple levels of decomposition. A part can consist of
parts, which again can consist of parts.

e Hierarchical structure between parts exists at both the operational and
the policy level. Users of business applications would like to specify the
parts an actual economic resource consists of, as well as the parts a re-


<!-- Page 132 -->


120 2 Structural Patterns at Policy Level

source type should consist of. A resource type can consist of multiple

instances of the same type, just as bicycle consists of two wheels (and

other parts).
Solution
The structure of a resource can be captured by the linkage relationship; see
Fig. 85. Linkage exists at two levels of abstraction. The linkage relations
hip between economic resources specifies their actual structure. The linka
ge type between economic resource types specifies the bill of material, a
compositional structure that characterizes all resources of the type.

An economic resource that contains other resources is called parent, and
economic resources contained in the other resources are called compon
ents. This terminology (inconsistent with the terminology used in objecto
riented software) has been standardized by the American Production and
Inventory Control Society (APICS) (Arnold 1998).

Economic parent

Resource Po linkage type

Type

component

specification
parent

Economic .

a meee
component
Fig. 85. Linkage

Linkage and linkage type are many-to-many relationships. A resource
can be used as a component in zero or more other resources, and a resource
can consist of zero or more other resources.

Like many other REA relationships, the linkage and linkage types relat
ionships also have properties. The Quantity Required property of linkage
type specifies how many components a parent should consist of, and the
Quantity Used property of linkage specifies how many components the
parent actually consists of.

Linkage type can be seen as a recipe to perform the transformation of
resources. A linkage type contains information about the structure of mater
ials and the tasks necessary to perform a transformation. A schedule adds
actual time intervals to this structure, and links it to actual resources and
economic agents that would be responsible for the transformation.


<!-- Page 133 -->


### 2.8 Linkage Pattern 121

tt
reer | yO
component 0..*
specification
parent 0..* /
Economic Linkage
Resource -
Fig. 86. Linkage in detail

There might exist several linkage types for an economic resource, which
basically means that we might have several recipes to produce the res
ource. For a given schedule, we must specify both the resource and the
linkage type we are planning to use for the transformation.

Examples

Bill of Material. A bill of material states precisely how much of each res
ource must be used or consumed to produce a given amount of another res
ource, and perhaps even in what order.

Work Breakdown Structure. A work breakdown structure is similar to
bill of material, but specifies activities or tasks necessary to perform a larg
er task or project.

Resulting Context

Business applications with linkage relationships can report on the variat
ions between the recipes and described procedures and the actual product
ion runs.

Users of business applications are expected to modify the linkage type
relationships over time to improve the transformation processes. This
might lead to the requirement in business applications for recording the
history of the changes of the linkage type relationship.

Sometimes it is not worth standardizing all the tasks in a work breakd
own structures. In some business situations it is easier to specify the exp
ected result (an economic resource type), and to rely on humans to do
what they know best.


<!-- Page 134 -->


122 2 Structural Patterns at Policy Level


### 2.9 Responsibility Pattern


Responsibility is a capacity for decisions, thoughts or actions

Context

In many cases, an economic agent is responsible for other economic

agents. For example, a manager is responsible for the employees in his or-

ganization, and the enterprise is responsible for the actions of its subsidiari
es.

Problem

How can we represent responsibility between economic agents in the REA

application model?

Forces

The solution needs to balance the following forces:

e An economic agent can change its responsibility to other economic
agents independently of the exchange or conversion processes with
which it is involved. For example, employees can change their reporting
relationships during their employment.

e Responsibility often determines the organizational structure of a comp
any. Employees report to their managers, who report to their managers,
and so on. However, there can be multiple organizational hierarchies. In
some organizations, employees reporting to their department manager
(this is often called “solid line reporting’’), are simultaneously members
of a team and report also to a team leader (this is often called “dashed
line reporting”’).


<!-- Page 135 -->


### 2.9 Responsibility Pattern 123


e Organizational structures significantly vary from one company to ano
ther. Many organizations consist of divisions, departments, and teams,
but there are a number of different organizational structures. The reas
ons for an the organizational structure are practical, such as a certain
limit of how many direct reports a manager is able to coordinate, rather
than things that can be derived from domain rules.

e The organizational structures as departments and teams can be indep
endent of the reporting relationships. A manager can establish several
teams from his direct subordinates, and there can be members of a sing
le team that report to different managers.

Solution

The responsibility relationship between economic agents describes a de-

pendency between two economic agents, in which the superordinate agents

are responsible for the economic events in which the subordinate agents

participate, see Fig. 87.

0..* superordinate
0..* subordinate

Fig. 87. Responsibility
Responsibility can be used to model the reporting relationship that

forms the organizational structure of the enterprise.

Organizational units such as departments and teams are more or less arb
itrary sets of economic agents, and we can model them in the REA applic
ation model as groups. The fact that an economic agent is a member of an
organizational unit is modeled as a grouping relationship. The grouping rel
ationship can also be applied between groups and models the hierarchical
structure of the organizational units; see Fig. 88.

Responsibility can also be used to model assignment, a relationship des
cribing, for example, that a salesperson is assigned to specific customers,
or a purchaser is assigned to specific vendors.


<!-- Page 136 -->


124 2 Structural Patterns at Policy Level
0..* superordinate unit
0..* subordinate unit
organizational unit
grouping
member
0..* reports to
semictom| | responsibility
0..* direct report
Fig. 88. Organizational units
Resulting Context
The responsibility relationship is not sufficient for modeling all aspects of
organizational structures. For example, a concept such as an open position
can be modeled as a labor type. Therefore, modeling the organizational
structure of a company requires creating an REA model for the company
that includes labor, labor type, and labor acquisition contract. This explains
why organizational structures differ so much from one organization to ano
ther; and why the full model of the organizational structure encompasses
several REA concepts.


<!-- Page 137 -->


### 2.10 Custody Pattern 125


### 2.10 Custody Pattern

4 a>...
. 5 a3 * all a
: tt want) = ses s AS ,
il Teed oe 5 ri
ao | faathasle! A : ie
— |
“hy
— - - |
Warehouse personnel have the custody of the goods in the warehouse
Context
Many companies make their employees responsible for specific resources.
Such information is useful if something happens to the resources and we
need to contact someone able to take care of the situation. This responsibili
ty does not directly imply any exchanges of resources, tasks, or labor the
employee performs with the resources he is responsible for; such cases
would be modeled as commitments or economic events. We talk about
general responsibility for things, such as cashiers in a shop responsible for
the cash in the cash register, or warehouse clerks responsible for the goods
stored in a warehouse.
Problem
In the REA framework, how do we model the responsibilities of economic
agents for specific economic resources?
Forces
Application developers may need to address the following forces:
e Some economic agents are responsible for economic resources. If this
responsibility is related to exchanges or conversions, it could be mode
led as an economic event. However, there are cases in which this re-


<!-- Page 138 -->


126 2 Structural Patterns at Policy Level
sponsibility has a longer term, and is not related to individual exchanges
or conversions.

e If something happens to specific economic resources, the users of busin
ess applications would like to get information about who to contact or
hold responsible for the resources.

e The economic agent responsible for the resources can be different from
any of the agents involved in conversions or exchanges. For example, a
manager of a gas station is responsible for the gas in the underground
tanks, although replenishing it is done by supply personnel, and dispensi
ng it is done by customers in self-service gas stations.

e There can be responsibility for a specific resource shared among several
economic agents.

Solution

The responsibility for economic resources is modeled in the REA frame-

work as a custody relationship between an economic agent and an eco-

nomic resource (when an individual agent is responsible for an individual
item), between an economic resource group and an economic agent group

(when a group of agents has shared responsibility for a group of re-

sources), between an economic resource group and an economic agent

(when an individual agent is responsible for a group of resources), or be-

tween economic resource and economic agent group (when a group of

agents has shared responsibility over an individual resource); see Fig. 89.

0..* custody 0..*

SF
Group ia Agent Group
custody 0..*
grouping grouping
0..* custody
0..*
Economic Economic
0..*
Fig. 89. Custody


<!-- Page 139 -->


### 2.10 Custody Pattern 127

Examples
The model in Fig. 90 specifies that warehouse managers should have res
ponsibility for the goods in the warehouse. Warehouse Managers is a
group of economic agents (even if the group has only one member, as we
explained in the chapter on groups and types). The Items in Warehouse is
the group of items that physically are located in the warehouse.

The custody type implies that there will be a specific custody link from
every internal agent instance in the group Warehouse Managers to every
item instance is in the group Items in Warehouse.

Items in Warehouse | 0..* 1.* Managers
grouping grouping
«economic «economic agent»
resource» Warehouse
Item Manager
Fig. 90. Custody of an economic agent to group of resources.
«group» «custody» ; Worchorce
:ltems in Warehouse Managers ~
«grouping» «grouping»
«grouping»
«resource» «grouping» «agent»
1001: Item Peter: Warehouse
Manager
j «resource»
: 1002: Item
f { «resource»
i / a 1003: Item
pA N «resource» |__| Location=Office IN
Fig. 91. Custody at runtime


<!-- Page 140 -->


128 2 Structural Patterns at Policy Level

The model in Fig. 91 illustrates instances of three Items, IOO1, 1002, and
1003, that have their location at the warehouse, and a Warehouse Manager
Peter has custody over these items, because he belongs to the group Wareh
ouse Managers. Peter does not have custody over the item 1004, which
does not have location in the warehouse, and therefore does not belong to
the group Items in Warehouse.
Resulting Context
The concept of custody allows users of business applications to plan,
monitor, and control the economic agents that have responsibility for spec
ific economic resources. For example, they can identify the economic
agent that has custody over a specific resource.

Custody is not an essential part of the model, as, for example, economic
event is. If custody is not used by business logic, and users of business app
lications are not interested in this information, it is simpler (i.e., better)
not to model custody in the application model.


<!-- Page 141 -->


3 An REA-Based Example Application
By Christian Vibe Scheller
In this chapter I will show you just how easy it is to use REA for developi
ng software applications. I will do so by developing a simple order webs
ite, where Joe’s customers can order pizzas. The finished webpage will
look like this:

Joe's Pizzeria

Order no. 10009

Your name John Doe Saetdor het

Your address {555 Bay Point

Kirkland, WA 98033

Item Quantity

Pizza Quattro Stagion v c| | Caddo order)

Total amount 8,95
Fig. 92. Joe’s web shop

The customer enters his order by first entering his name and address.

This allows Joe’s Pizzeria to know where to deliver the pizzas and to
whom. If the customer is already registered in the system, he can press the
link labeled “already a customer?” This will cause the web page to display
the customer’s address without the customer having to type it himself.


<!-- Page 142 -->


130 3 An REA-Based Example Application

The customer proceeds to enter his order by specifying which pizzas he
wants to order and how many. The web page responds by calculating the
total amount the customer has to pay for the order.

Finally, the customer presses the submit button. Only then will all the
order information be stored in the database. In a real web application the
customer would then have to specify credit card information, etc., but we
will skip this part for the sake of simplicity.


### 3.1 Representing the Metamodel

A special concern when implementing an application based on the REA
model is that the REA model exists on two separate levels of abstraction
(the application model and the metamodel).

As a general rule we should not mix two levels of abstraction in the
same source code. While it is possible to do so in programming languages
that support reflection, it is almost always the case that the reflection code
and the reflected code resides in different components.

We need to make a choice: If we implement the application model, we
will just have to map the concepts of the metamodel as well as possible to
the existing metamodel of the programming language (e.g. by using inherit
ance to represent metamodel elements or by using attributes to describe
metadata). If we implement the metamodel however, the application model
becomes data and we are basically developing our own programming lang
uage.

I can see the benefits of both approaches: I find that the first approach is
easy to explain and understand whereas most developers get scared by the
second approach. The second approach, however, results in a model that
captures the deep knowledge of the business model in a much more prof
ound way.

We will look into the approach of implementing the metamodel in chapt
er An Aspect-Based Example Application at the end of Part II of this book,
but for now we will stick to implementing the application model.


### 3.2 Component Model


Let us start out by defining the components that we want to build our app
lication from. The dependencies between the different components are
shown in Fig. 93.


<!-- Page 143 -->


### 3.2 Component Model 131

Joe’s Web pom
Data Access Layer
Domain Model |-----------~ Leen Database
REA Model OLAP
Fig. 93. Component model of the REA sample application

REA model defines the underlying REA model. Classes such as Order
and Customer will inherit from base classes defined in this component.
The REA model component will be designed with reusability in mind, so it
can be reused in other REA-based applications.

Domain model contains all the entities that make up Joe’s Pizzeria. In a
real-life application the domain model would contain everything including
purchase, production, salaries, etc., but in our small sample application we
will only model sales orders and customers. We will make the design rule
that all classes in the domain model must inherit from one of the base
classes in the REA model component.

Joe’s Web is the actual web site that the customers will be visiting when
they want to order pizzas. Joe’s web consists of a number of web pages
running on a web server. As a design rule we will not put any business
logic directly in this component. All the business logic will instead be
placed in the domain model and REA model components.

Data Access Layer is responsible for retrieving objects from the datab
ase as well as storing objects in the database. The process of transforming
a domain object to its database equivalent is often referred to as O/R mapp
ing. While O/R mapping tools exist, in the case of this simple web applic
ation we will just be writing the code ourselves.

Database is where the data (orders, customers, etc.) gets stored. The dat
abase is the only persistent component in the application, so if we want
our data to be available over time we need to put it in the database.

OLAP — In our sample application we would like to provide Joe with all
kinds of information about his business: What kinds of pizzas are the most
popular? Are sales going up or down? Etc. In my opinion an OLAP cube is
the ideal tool for this kind of information.


<!-- Page 144 -->


132. 3 An REA-Based Example Application
{ Contract B:
| Abstract Class
| Fields
i @ ID
{ fl Methods
i =@ Contract
@ = IncrementCommitments | @ DecrementCommitments
( IncrementComm... BB: f DecrementComm... a:
| Abstract Class | Abstract Class H
| Fields i & Fields
{ — @ Amount : @ Amount
i Fulfilled | @ Fulfilled
| El Methods | © Methods
{| =@ Fulfill @ Resource =| a Fulfil
| Events | & Events
| § EventCreated | § EventCreated
@ Recipient | @ Resource
@ Provider
| Abstract Class » Abstract Class
| B Fields | El Fields
i @ ID i @ ID
i @ Name i @ Name
i I Methods i @ Value
| = Agent ny ny ae
ny anne @ Resource
# Provider DecrementEvent [§
Class
IncrementEvent | ¥
Class
a =) Fields
© Fields @ = Recipi... ; Amount
@ Amount ane
3 Date Reso...
Fig. 94. REA Model Component


<!-- Page 145 -->


### 3.3 The REA Model Component 133

3.3. The REA Model Component
Fig. 93 shows the REA model that we will be basing our application on.
As can be seen, the model is not a complete REA model. This is because
we don’t need concepts such as duality in our sample application. The
simplification of the REA model is a pattern in itself called MODELING
COMPROMISE.

Each object in the REA model is defined as an abstract base class. When
we later define our domain model, each of the domain objects is going to
inherit from one of these base classes. The exception to this rule is the
Event class, which does not have a domain counterpart.

As can be seen from the diagram, the Agent class has two fields. The
first field is the JD which is a unique identifier for the Agent class. The
main purpose of the JD field is to identify the agent record in the database
as well as to solve the ambiguity that would otherwise occur if two agents
were to have the same name. The Name field is also a kind of identifier of
the agent but it is less strict than the JD in that it is not necessarily unique.
On the other hand the Name is the identifier that humans use: “Did John
Doe receive his pizzas?” Joe might ask. Anyway, here is the code:
public abstract class Agent {

public int ID;
, public string Name;

Just like agents, Resources contain an ID and Name. In addition a res
ource has a Value which is defined as the value in US dollars of a single
unit of the resource, i.e., the price of a single pizza:
public abstract class Resource {

public int ID;
public string Name;
, public double Value;

The Contract class contains an ID field and two collections: A collect
ion of increment commitments and a collection of decrement commitm
ents.
public abstract class Contract {

public int ID;
public List<IncrementCommitment> IncrementCommitments = ..
public List<DecrementCommitment> DecrementCommitments = ..


<!-- Page 146 -->


134 3 An REA-Based Example Application

First of all it is worth noting that the Increment Commitment class,
unlike the Agent, Resource and Contract classes, does not have an ID. This
is because commitments do not have identities — after all what is the diff
erence between receiving ten dollars and receiving five dollars and then
another five dollars? Another thing worth noting is that the commitment
classes contain a fulfillment mechanism:

Once a certain commitment is fulfilled, the application can call the
commitment object’s Fulfill() method. This will cause the commitment to
change its Fulfilled field to true and will also cause the commitment to
generate an economic event based on its own information. Since the comm
itment itself does not know what to do with this economic event, it will
pass it to the calling application using the EventCreated delegate.
public abstract class IncrementCommitment {

public Resource Resource;

public double Amount;

public Agent Provider?

public bool Fulfilled = false;

public event IncrementEventCreatedHandler EventCreatedi

public void Fulfill() {
Fulfilled = truei
IncrementEvent e = new IncrementEvent (this) 7
EventCreated(e) i

}

}

Basically, decrement commitments are identical to increment commitm
ents except they have a recipient instead of a provider. While writing this
chapter I was debating with Pavel whether decrement and increment comm
itments should actually be modeled as different classes or if they should
rather be merged into a single generic commitment class. In the end we
decided that the semantic difference between the two types of commitm
ents is so important to the whole REA model that they should be kept
separate.

The Increment Event and Decrement Event will be generated by the
REA model component whenever a commitment is marked as fulfilled by
calling its Fulfill() method.
public class IncrementEvent {

public DateTime Date;

public Resource Resource;

public double Amount;

public Agent Provider;

public IncrementEvent(IncrementCommitment commitment) {
Date = DateTime.Now;
Resource = commitment.Resourcei


<!-- Page 147 -->


### 3.3 The REA Model Component 135

Amount = commitment .Amounti
Provider = commitment .Provideri
}
| Agent | Customer |
| Abstract Class ‘ Class
| TX + Agent
T
er Fields
3 Address
Pizza | |
a Class
| Resource a > Resource
1 Abstract Class N
; F<
Class
-* Resource
T
=| Properties
=F USD
f Contract | iE Order |
| Abstract Class Class
a] TN + Contract
MC =| Properties
= Total
= Methods
=8 AddPayment
f IncrementComm... i PaymentLine |
| Abstract Class Class
| FN > IncrementCommitment
{ DecrementComm... (i OrderLine a
| Abstract Class Class
= WN + DecrementCommitment
Fig. 95. Domain Model Component


<!-- Page 148 -->


136 3 An REA-Based Example Application


### 3.4 The Domain Model Component


The diagram in Fig. 95 shows the domain model component. It is worth
noting that the model does not contain any associations between domain
classes. This is because all the associations are inherited from the REA
model component. It can also be seen that each domain class inherits from
a corresponding REA class.

A Customer is basically an agent. An Address field has been added so
that Joe will know where to deliver the Pizzas.
public class Customer : Agent {

public string Address?
}

Pizzas are resources.
public class Pizza : Resource {
}

The Currency class is needed because the REA model expects every
commitment and event to have a Resource. The Currency class represents
monetary value. In reality, only one type of currency will be used in the
application, namely US dollars, so we implement a singleton pattern.
public class Currency : Resource {

private Currency() {}
public static Currency USD {
get {
Currency usd = new Currency()i
usd.ID = 0;
usd.Name = "USD";
usd.Value = 1i
return usdi
}
}
}

An Order Line is a decrement commitment where Joe’s Pizzeria comm
its itself to deliver a given number of pizzas of a specific type to a cust
omer.
public class OrderLine : DecrementCommitment {

}

A Payment is an increment commitment where the Customer commits

himself to pay Joe’s Pizzeria a certain amount of currency.


<!-- Page 149 -->


### 3.5 The Database 137

public class PaymentLine : IncrementCommitment {

}

The Order class is the only class in the domain model component that
adds something that could reasonably be called business logic. The order is
able to calculate the total amount (in USD) that the customer should pay
for his pizzas. The order can also add a payment line based on this total to
its incoming commitments.
public class Order : Contract {

public double Total {
get {
double total = 0;
foreach (OrderLine line in DecrementCommitments) {
total += line.Amount * line.Resource.Valuei
}
return total;
}
}
public void AddPayment (Customer customer, Currency currency) {
IncrementCommitments.Clear()i
PaymentLine line = new PaymentLine()ji
line.Amount = Totali
line.Resource = currency?
line.Provider = customer;
IncrementCommitments.Add(line) i
}

}

All in all the domain model component consists of only 28 lines of code
(not including blank lines and closing brackets).


### 3.5 The Database


The database is designed to mimic the domain model as closely as possi-

ble, see Fig. 96. All fields have the same name and are of the same data

type as in the domain model. A few exceptions are necessary, however,
due to the nature of databases:

e In the domain model order lines and payment lines are part of an order.
In the database this is modeled by adding an order ID to each order line
and payment line.

e In the domain model resources, providers and recipients are references
to resource and agent objects. In the database, resource ID, provider ID
or recipient ID are foreign keys to the pizza and customer tables.


<!-- Page 150 -->


138 3 An REA-Based Example Application
Order
§ 3
PaymentLine OrderLine
G | OrderID @ | OrderID Pizza
@ | Provider G | Recipient eee |) fc
Amount @ | Resource Name
Fulfilled Amount [Value]
i Fulfilled i
¢ y 5
IncrementEvent Customer Decrementévent
B [Date] po—OrI gD easel
| Provider Name ac On e Resource
Amount Address Recipient
Amount
Fig. 96. The database

### 3.6 The Data Access Layer

The data access layer contains a single static class with a number of metho
ds for retrieving and saving data to the database, see Fig. 97.

These methods are extremely simple so I will not waste too much space
listing all the code. Here is a single example showing the code for the GetP
izzas() method:
public static Dictionary<int, Pizza> GetPizzas() {

Dictionary<int, Pizza> pizzas = new Dictionary<int, Pizza>()i
using (SqlConnection connection = new SqlConnection("..")) {
connection.Open() i
SqlCommand command = new SqlCommand("select number, name,
price from pizza", connection);
SqlDataReader reader = command.ExecuteReader();
while (reader.Read()) {
Pizza pizza = new Pizza()i
pizza.ID = reader.GetInt32(0);
pizza.Name = reader.GetString(1)j;
pizza.Price = (double) reader.GetDecimal (2)
pizzas.Add(pizza.ID, pizza)i
}
}
return pizzasi
}


<!-- Page 151 -->


### 3.7 Joe’s Web = 139

( Facade a
\ Static Class I
I Fi
H = Methods |
1% GetCustomers
\ =6 GetOrders 1
| = GetPizzas I
| =& SaveCustomer
\ 26 SaveDecrementEvent !
1 = SavelIncrementEvent
\ =6 SaveOrder I
| = SaveOrderLine
| 6 SavePaymentLine i
a
Fig. 97. The data access layer
One of the interesting features of the data access layer is that it is res
ponsible for saving the economic events generated by the commitments. It
does so by attaching an event handler to the order lines and payment lines
in the GetOrders() method:
public static Dictionary<int, Order> GetOrders() {
OrderLine line = new OrderLine()i
order .DecrementCommitments.Add(line) i
line.EventCreated +=
new DecrementEventCreatedHandler (OrderLine_EventCreated) i
PaymentLine line = new PaymentLine()ji
order .IncrementCommitments.Add(line) i
line.EventCreated +=
new IncrementEventCreatedHandler (PaymentLine_EventCreated) i
}
static void OrderLine_EventCreated(DecrementEvent e) {
SaveDecrementEvent(e) i
}
static void PaymentLine_EventCreated(IncrementEvent e) {
SaveIncrementEvent(e) i
}

### 3.7 Joe’s Web

Now that all the underlying components are in place we are ready to dev
elop the user interface.


<!-- Page 152 -->


140 3 An REA-Based Example Application

The order web page is developed in ASP.Net and uses the page’s ViewS
tate to store the order and customer objects between post backs. This is
extremely convenient when you base your development on a domain
model.
public partial class CreateOrder : System.Web.UI.Page {

Order Order;
Customer Customer;
protected void Page_Load(object sender, EventArgs e) {
if (!IsPostBack) {
Order = new Order(Facade.GetNextOrderID());
OrderNumberLabel.Text = Order.ID.ToString()ji
Customer = new Customer (Facade.GetNextCustomerID())i
foreach (Pizza pizza in Facade.GetPizzas().Values) {
ListItem item =
new ListItem(pizza.Name, pizza.ID.ToString())
ResourceList.Items.Add(item) i
}
ViewState.Add("order", Order) i
ViewState.Add("customer", Customer) j
} else {
Order = (Order) ViewState["order"]i
Customer = (Customer)ViewState["customer"];
foreach (OrderLine line in Order.DecrementCommitments) {
AddOrderLineTableRow(line) i
}
}
}

If the user presses the Already a customer link, see Fig. 92, the web
page will search the database for a customer with the correct name and
then use that customer as the recipient for the order lines. The web page
will also display the customer’s address information:
protected void AlreadyCustomer_Click(object sender, EventArgs e) {

foreach (Customer customer in Facade.GetCustomers().Values) {
if (customer.Name == NameTextBox.Text) {
Customer = customer;
AddressTextBox.Text = customer .Addressi
ViewState.Add("customer", Customer) j
breaki
}
}
}

When the user presses the add to order button, the web page will genera
te an order line based on the information that the user has entered and
then add that order line to the order object:


<!-- Page 153 -->


### 3.8 The Fulfillment Page 141

protected void AddToOrder_Click(object sender, EventArgs e) {
OrderLine line = new OrderLine()i
line.Amount = double.Parse(QuantityTextBox.Text ) i
line.Resource = Facade.GetPizzas()[ResourceList.SelectedValue] i
line.Recipient = Customer;
Order .DecrementCommitments.Add(line);
AddOrderLineTableRow(line) i
ViewState.Add("order", Order) i
TotalAmountLabel.Text = Order.Total.ToString("#.00")7
}
The final piece of code that we need for our web page is the code behind
the Submit your order button:
protected void Submit_Click(object sender, EventArgs e) {
Order .AddPayment (Customer, Currency.USD) i
Customer.Name = NameTextBox.Text;
Customer.Address = AddressTextBox.Texti
Facade. SaveCustomer (Customer) j
Facade. SaveOrder (Order) i
Response.Redirect("MainPage.aspx") i
}
Now everything is in place and Joe is ready to receive orders from his
customers.

### 3.8 The Fulfillment Page

Once the customer has submitted the order, Joe needs to keep track of it.
He needs to know whether the customer has received his pizzas and
whether he has paid for them or not. For this purpose the system contains a
fulfillment page, illustrated in Fig. 98.


<!-- Page 154 -->


142 3 An REA-Based Example Application
Order no. 10022
Order lines
Resource Quantity Recipient Fulfilled
Pizza Salsiccia 2 John Doe O
Pizza Polloe Pesto 3 John Doe O
Payment
Resource Amount Provider Fulfilled
USD 55.75 John Doe 0

Fig. 98. The fulfillment web page

By checking the checkboxes, Joe can mark a specific order line or paym
ent line as Fulfilled. The fulfillment page supports scenarios where the
customer pays up front for his pizzas as well as scenarios where the cust
omer pays on delivery. At least in the area where I live, both these scenari
os occur regularly.

Less realistic is the fact that Joe can partly fulfill an order, but only by
providing all the pizzas of a specific type at once. This flaw is caused by
the simplified fulfillment mechanism we implemented in the REA model.

Behind the scenes the fulfillment page is using the same domain model,
data access layer and database as the order web page. When Joe presses
the Save Changes button, the web page runs through all checkboxes and
calls the associated order line or payment line’s fulfill method if necessary:


<!-- Page 155 -->


### 3.9 The OLAP Cube 143

protected void SaveChanges_Click(object sender, EventArgs e) {
for(int i=0; i < OrderLineTable.Rows.Counti i++) {
CheckBox checkbox =
(CheckBox) OrderLineTable.Rows[i].Cells[3].Controls[0]i
if (checkbox.Checked &&
!Order .DecrementCommitments[i].Fulfilled) {
Order .DecrementCommitments[i].Fulfill()i
}
}
for (int i = 07 i < PaymentLineTable.Rows.Count; i++) {
CheckBox checkbox =
(CheckBox) PaymentLineTable.Rows[i].Cells[3].Controls[0];
if (checkbox.Checked &&
!Order.IncrementCommitments[i].Fulfilled) {
Order .IncrementCommitments[i].Fulfill()i
}
}
Facade. SaveOrder (Order ) i
Response.Redirect ("MainPage.aspx") i
}
This eventually causes decrement events and increment events to be
stored in the database, see Fig. 99:
M DecrementEvent : Table Ce®)
| | Date __| ResourcelD | RecipientiD [Amount [J «
>| 15 1002 1
ff 8/1/2005 13 1002 1
Oo 6/13/2005 10 1002 3
Oo 10/11/2005 11 1002 2
- | 4/13/2005 12 1001 1
10/19/2005 12 1002 1
oO 9/18/2005 10 1003 5
Oo 6/20/2005 15 1005 2
Oo 3/4/2005 10 1004 2
Oo 7/15/2005 11 1002 2
a 8/17/2005 13 1001 3
GB 9/7/2005 12 1003 1
i 11/13/2005 10 1004 1
Oo 9/8/2005 1 1005
Record: (aj af 7 1|> [>i j>#) : ‘
Fig. 99. Decrement event table

### 3.9 The OLAP Cube

Now it is time to generate some management reports based on our event
data.


<!-- Page 156 -->


144 3 An REA-Based Example Application
To make it really simple let us just add a simple Microsoft Access pivot
table on top of each of the event tables. While this is not a real OLAP cube
it still provides us with the same basic functionality.
The definition of the cube based on decrement events is in Fig. 100.
gE" DecrementCube - Select Query C ex)
SELECT DecrementEvent.Date, Customer.Name AS Customer, Pizza.Name AS Pizza, a
DecrementEvent.Amount a
FROM Pizza INNER JOIN (Customer INNER JOIN DecrementEvent ON Customer.ID =
pueseaatam ON Pizza.ID = DecrementEvent.ResourcelD;
v
Fig. 100. Definition of the decrement event table
We can use this cube to get simple sales statistics based on Joe’s pizza
sales.
iE" DecrementCube : Select Query C eX)
Pizza ~ Sum of Amount
PizzaAllaRomana + 2109
Pizza Bufalina = 969
Pizza Margherita ei 2372
PizzaPolloePesto + 84
Pizza Quattro Stagioni* 740
Pizza Salsiccia 1444
Pizza Vegetariana = 3366
Grand Total = 11084
Fig. 101. Pizza sales
It is probably easier to see the results if we present them as a bar chart in
Fig. 102.


<!-- Page 157 -->


### 3.9 The OLAP Cube = 145

i DecrementCube : Select Query Cex)
4000
3500
3000
8 2500
s 2000
=
= 4500
=
1000
500
0
fenene Pizza Bufalina Maegpodis — tere Oeiaiects Vepelodane
tem
Fig. 102. Pizza sales bar chart
Based on these figures Joe should probably remove the Pizza Pollo e
Pesto from his menu and instead consider adding more vegetarian pizzas.
We can also have a look at the increment events in Fig. 103.
gE IncrementCube : Select Query Cie)
Years > Months Sum of Amount
©2005 ‘BJan * 2404.85
Feb + 5114.95
Mar + 71777
BApr * 9402.2
May * 9399.85
@Jun * 10147.15
BJul + 10135.35
BAug * 11054.8
& Sep + 9664.8
Oct * 10177.2
BNov + 12057.25
Dec * 12390.95
Total * 109127.05
Grand Total = 109127.05
Fig. 103. Cash receipts
Again let’s look at the data as a bar chart in Fig. 104.


<!-- Page 158 -->


146 3 An REA-Based Example Application

iE" IncrementCube : Select Query Cee)
_ 10000
a
2 ot BOORUerat
=) 8000
i. OURO
B 6000
3
2 ot SOOT

ae ooo
MiIRRRRRRRR RI
0
Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec
2005
Month

Fig. 104. Cash receipts as bar chart
All in all it looks as if things are going well for Joe: sales have been

steadily increasing over the year.


### 3.10 Conclusions


Hopefully this example application has shown that it is indeed simple to

develop an REA-based business application. The main benefits of doing so

are:

e By basing the domain model on a proven and well-understood core
model (the REA model) we minimize the risk of design flaws in our app
lication. By demanding that all domain classes inherit from base
classes in the REA model we are able to perform a design-time check
that the domain model is consistent.

e Due to the fact that we base our domain model on a model that covers a
larger set of business cases than the domain model itself, it is relatively
easy to extend the domain model at a later time. If for instance Joe dec
ides to track usage of raw materials for making pizza, we know that
this will easily fit into the model.


<!-- Page 159 -->


### 3.10 Conclusions 147


e Because much of the business logic resides in the reusable REA model
we can minimize the development effort. In the example application we
were able to create a complete domain model for the pizza sales applicat
ion with only 28 lines of code.

While I strongly recommend that you start using the REA-model there
are of course also some caveats that you need to take into consideration:

e If you are developing an application that really is not about resources,
events and agents (for instance a document management system), you
may end up spending a lot of time trying to “shoehorn” the application
into the REA model. It is important to decide early on whether the REA
model is applicable.

e While the REA model is very powerful it is also very abstract. If you try
to explain your design to a customer or fellow employee, you may find
that explaining the underlying REA model is difficult. Trying to hide the
fact that you are basing your design on an REA model may also be a
bad idea, because major design decisions are based on the decision to
use REA (e.g., why should the customer ID be placed on each order line
instead of on the order itself).


<!-- Page 160 -->


# Part Il Behavioral Patterns


The previous part, Structural Patterns, discussed the structure of a business
application, which conforms to the laws of the business domain, consisting
of REA entities and their relationships. To build a useful business applicat
ion, this structure is only one of the things an application developer has to
determine. Users of business applications usually require additional funct
ionality, such as serial numbers, accounts, price calculations, and convers
ions between units of measure. This functionality is essential in some app
lications, but it might not be required in others. All depends on the users
of a business application, actual configuration of an application, and the
common practices in their businesses.

In this part, Behavioral Patterns, we describe how the REA model can
be extended to support specific functionality that originates in user req
uirements.

Behavior Customizable Functionality
IDENTIFICATION DUE DATE DESCRIPTION NOTE
identity of entities deadlines external internal
CLASSIFICATION LOCATION NOTIFICATION MAT ERIM

grouping where events occur message invoices
POSTING ACCOUNT RECONCILIATION VALUE
keep history retrieve history match transactions units of measure
INVENTOR’S PARADOX
how to discover new behavioral patterns

REA Structure at Policy Level Extended Skeleton

What Could, Should or Should not Happen

REA Structure at Operational Level Fundamental Skeleton

What Has Happened


<!-- Page 161 -->


4 Cross-Cutting Concerns


### 4.1 Behavior May Not Be Localizable Into REA Entities

Units of functionality that extend the REA model are usually not localiza
ble into a single REA entity. An example is illustrated in Fig. 105. This
example shows the economic resource Vehicle, which belongs to the Vehic
le Category, and is used in the economic events Trip.

a
Vehicle «grouping» resource» «use» event»
Category Vehicle Trip

Rules Number
EE Odomete:
aa =o =o
Lo |
Fig. 105. Behavioral patterns often crosscut REA entities

A License Plate Number of a vehicle is an attribute of the economic res
ource Vehicle. The License Plate Number is usually not a random numb
er. It is constructed using a License Plate Rules, which is a property of
Vehicle Category (for example, numbers of police cars, military cars, and
diplomatic cars are constructed using different rules than numbers of other
cars). The property License Plate Rules contains rules specifying the
uniqueness of the License Plate Number, its format, its dependency on
previous numbers or other attributes, and so on. Therefore, the unit of
functionality of a License Plate Number Series is present on two REA entit
ies, the resource and the resource group, and the number is constructed by
mutual collaboration between the part that resides on the resource and the
part that resides on the group.

Likewise, a Mileage of a Vehicle is calculated as the aggregated number
of the trip Distances the vehicle traveled. As Trip is an economic event,
the Odometer is a unit of functionality present on two REA entities, the
economic resource and the economic event.


<!-- Page 162 -->


152. 4 Cross-Cutting Concerns

It is still useful to think about a License Plate Number Series, and about
an Odometer as single units of functionality, but these units span several
REA entities.

We will use aspect-oriented programming as a conceptual framework
and a convention of thought for modeling the crosscutting modules of
functionality. Aspect-oriented programming is one of the mechanisms for
describing the crosscutting features and manipulating them as modular
units. Aspect-oriented programming is based on the ideas of Gregor Kiczal
es, John Lamping, Anurag Mendhekar, Chris Maeda, Cristina Videira
Lopes, Jean-Marc Loingtier, and John Irwin, (Kiczales 1996). This group
at the Palo Alto Research Center, a subsidiary of Xerox Corporation, dev
eloped a general purpose aspect-oriented language called AspectJ, an ext
ension of the Java programming language with aspect-oriented features.
Many other research centers have developed other aspect-oriented lang
uages, both general purpose and specific to a certain domain.

At the end of Part II of this book we illustrate two ways of implementi
ng the behavioral patterns, one in C# code, and the other using a model
framework. Nevertheless, the behavioral patterns, as described in this
book, can be also used without a specific implementation in mind, or imp
lemented in another way.

To stay independent of any particular implementation, we call Aspects
the crosscutting units of functionality, such as License Plate Number Ser
ies, and Aspect Elements the units that are present on REA entities, such
as License Plate Rule, License Plate Number, Mileage, and Distance.


### 4.2 Framework-Based Approach


Aspect-oriented languages that are not framework-based, such as AspectJ,
express the structure of a software application in the form of code in the
programming language, and the crosscutting concerns, or aspects, are also
expressed as code. During compilation, both the application code and the
aspect code are combined together in a process called weaving; see
Fig. 106.


<!-- Page 163 -->


### 4.2 Framework-Based Approach 153

Code in Java ms
Code in Java
Fig. 106. Aspect-oriented programming at the code level (not framework-based)

Keeping in mind requirements such as extensibility and configurability,
a disadvantage of such programming languages is that the code has to be
weaved (which means recompiled) every time the functionality of an app
lication (expressed as application code or aspect code) changes. The cons
equence is that upgrading an application is complicated and expensive.

Furthermore, since some or all functionality of an object is provided in
the aspect code, it is impossible for the weaver to guarantee a system-wide
quality for an application, because the weaver has no way of knowing what
the aspect code does.

To satisfy the requirements for extensibility, configurability, and upg
radeability, we use the framework approach to model and implement the
aspects. Every aspect is represented at two levels of abstraction, the Aspect
type level and the Application model level; see Fig. 107.

We will use a simplified version of the IDENTIFICATION PATTERN as
an example. The IDENTIFICATION PATTERN encapsulates business
logic for providing identity to REA entities, such as serial numbers and
names. Details about the identification pattern are described in the
IDENTIFICATION PATTERN chapter.

The Aspect Type level specifies the types of the aspects, and metadata
that can be applied to the aspects in the application model. This level enc
apsulates the business logic of the aspect, and specifies the configuration
properties, which can be set by application developers. In the example ill
ustrated in Fig. 107, the Identification Aspect consists of two element
types, Identifier Type and Identifier Setup Type. The cardinality of the
composition indicates that instances of these types (i.e., Identifier and
Identifier Setup) can be configured in the application model several times.
These two elements are related by a one-to-many relationship, which indic
ates that for each configured identification aspect in the application
model, for one Identifier there can be exactly one Identifier Setup, and for
one Identifier Setup there can be one or more Identifiers.


<!-- Page 164 -->


154 4 Cross-Cutting Concerns
The Application Model level specifies the runtime attributes that can be
set by the users of business applications or automatically by the system.
The application model level also specifies which aspects are configured on
which REA entities; in Fig. 107 the REA entities are shown by dashed
lines, to indicate that they are not part of the aspect. An REA entity of a
type group can contain zero or more Identifier Setup aspect elements, and
any REA entity can contain zero or more Identifier elements. For each
Identifier instance at runtime, there is exactly one Identifier Setup instance;
for each Identifier Setup instance at runtime, there can be zero or more
Identifier instances.
Aspect T ‘ficath
pect Type [- indentification Name ofthe [A
oO pect | | aspect type
Name --00
Name of the Aspect Pattern
AutoNumber (Y/N) ----...__ | Configuration |
Unique (Y/N) _~-~~~}~~-~-~---1_ properties of the
Types of aspect NN Mandatory (Y/N) “"y aspect type
elements that can be
configured on REA 14 *
entities Pm
“~./ Identifier | Identifier
Type Setup Type
Application Model a:
Pp ainstanceOf» “i"stanceOf» Elements of the |X
; aspect that can be
H ' | Configured on REA
ans! 0." Identifier Setup entities
Group a ID rule na va
rr ' Last Number ~~ |”
REA Entities on which]. —_— _
aspect elements can be ' 1 oA Properties that [\
configured 0% can be set at
7 ' ra _[tuntime
ntity a Identification String -|~
Any REA entity can have [\
several identifiers
Fig. 107. Aspect pattern in the framework approach


<!-- Page 165 -->


### 4.2 Framework-Based Approach 155

In the examples illustrating application models with aspects, we use the
notation in Fig. 108. The Aspect Elements are shown as rectangles with
thick line; their runtime properties are shown similarly, as UML attributes.
Properties of the aspect element types (i.e. the properties whose values are
set at the aspect type level) are shown in the name compartment of the asp
ect. Values of the properties of the aspect type are shown as text close to
the line connecting the aspect elements, similarly as UML attributes; for
example, ‘Mandatory = yes’.
«identifier Setup» “~~ c-{ Aspectetement —S
; Order Number Setup
JID Rule
Properties of the [\ «ldentification» <r
element type set at Number Series IN
design time are Mandatory = yes~~....._
shown in the upper Unique = yes ace
compartment AutoNumber = yes ~ =" Aspect pater properties TS
‘ «ldentifiery ——————- ~s Aspect element ype name OS
Fig. 108. Notation used for aspects in application models
Model in Fig. 109 illustrates a fragment of an REA model with two
REA entities, a contract Sales Order and a group Orders. Application dev
elopers would like to implement sales order number on the Sales Order
entity. They decide to configure the identification aspect on the Sales Ord
er. The result is illustrated in Fig. 110.
«group»
Orders
«grouping»
«contract»
Sales Order
Fig. 109. Fragment of an REA model without aspects


<!-- Page 166 -->


156 4 Cross-Cutting Concerns

The Identification Aspect Pattern has the name Number Series. The conf
iguration parameters Mandatory, Unique, and AutoNumber are all set to
yes. The Identifier Setup element is called Order Number Setup, and the
Identifier is called Order Number.

be set at runtime ee
ae Order Number Setup -fF}"
«group» os.
= |
/ . ID Rule a

REA entities N «identification» _ Rare of Aspect TS

identification ry = yes ~

Aspect Unique = yes ae mm

1 AutoNumber = yes ~~~.
\ ccontract» eo set at design time
set at runtime
Fig. 110. Fragment of an REA model with identification aspect

Advantage of a system with explicitly modeled aspect types is that
software business applications are much easier to configure, customize and
upgrade than if the aspects were to be represented only as code in a prog
ramming language.

Configuration of software business applications using the aspect patt
erns is basically reduced to creating an REA model, setting the configurat
ion parameters of aspects, and specifying which aspects are present on
which objects. This can be done without writing any code in a programm
ing language.

Software applications are easy to customize, as the customization task
basically comprises setting up the configuration parameters of the aspects.

Furthermore, software applications are easy to upgrade, because all app
lication logic is encapsulated in the elements at the aspect type level, and
it can be extended independently of the configured application model. The
upgrade of the software application basically means replacing the elements
at the aspect type level with elements with upgraded functionality. The


<!-- Page 167 -->


### 4.3 There Is No Complete List of Behavioral Patterns 157

framework developer designs the interface (the configuration properties
and the corresponding behavior) that the elements at the aspect type level
expose. If the upgraded elements support the old interface, the software
applications can be upgraded without reweaving or recompiling the applic
ation.

Even if the upgraded elements are not backwards compatible (backward
compatibility is considered anti-pattern by some practitioners), it is possib
le to write an upgrade script that modifies the configured applications to
support the upgraded elements.

Quality of the software applications is easier to control, as all functiona
lity of business applications is encapsulated in a framework, and is theref
ore tested by framework developers. The framework developers, who
provide the elements at the aspect type level, have full control over what
application developers may do with their aspect elements. In other words,
providing application developers a domain-specific modeling language red
uces the number of errors the application developers can make, compared
to the situation in which the application developers write code in a general
programming language.


### 4.3 There Is No Complete List of Behavioral Patterns

While with the structural patterns our aim was to find the minimal, yet
complete set of abstractions covering the business domain, this is not poss
ible with behavioral patterns. Users of business applications will always
need new features, and behavioral patterns provide a mechanism to add
new features to a business application without changing its fundamental
structure.

There are behavioral patterns waiting to be discovered. This section des
cribes the patterns we came across in building our business solutions, but
it is not a complete list of all patterns that might be needed in any line of
business. As the REA structural patterns define more or less a complete set
of concepts, if application developers identify user requirements for new
functionality, they would likely be either new behavioral patterns which
crosscut the REA entities or features in a domain other than the business
domain.


<!-- Page 168 -->


5 Patterns

### 5.1 Identification Pattern

j ry =)
Uys.
So 1};

Sov
Barcode is a machine readable strip for automatic identification of items,
by means of printed bars of different widths
Context
People refer to real or imaginary things by their names. We name things to
identify them, so we can refer to them by their names and not just point to
them and say ”this!”. By naming, we give things identities, but in real life
they are not often unique. Many things have more than one name, and
sometimes a single name can refer to different things, which is fine as long
as everyone who uses that name knows what thing it refers to. In business,
people use serial numbers, production numbers, civil registration numbers,
and names.
Problem
How do we specify the identity of things represented by REA entities?
Forces
The solution needs to balance the following forces:


<!-- Page 169 -->


160 5 Patterns

e An identity is a given feature; it is not an intrinsic part of the objects and
things. Therefore, an REA application model must specify whether there
is a business reason requiring REA entities to have a distinct identity,
and how that identity is modeled. We could omit modeling identity of
an entity, but then we could distinguish different instances of this entity
only by the values of their attributes.

e Users of business applications do not necessarily require that all REA
entities have an explicit identifier. For example, users of business applic
ations might not be interested in managing the identifiers of sales order
lines.

e Some identifiers are unique in the universe, such as the GUID (Global
Unique Identifier); some are not unique, such as the first name and last
name of a person. Some identifiers are unique within a certain group,
such as a serial number, which is unique within the group of entities that
belong to the same number series.

e There are specific rules on how to construct identifiers. For example, the
ISBN (International Standard Book Number) or the numbers of major
credit cards are constructed in a way that enables verifying, using a simp
le calculation algorithm, whether the number is valid.

Solution

The Identification Aspect Pattern can be used in situations in which appli-

cation developers want to specify the identity of REA entities.

In the REA application model, the Identification Aspect consists of two
elements. The /dentifier element represents the name or number of an REA
entity. The Identifier Setup element specifies the rules for creating the
Identifiers.

The Identifier Setup is often configured on group of REA entities that
share the same rules for creating identifiers, for example, on a group that
belongs to the same number series. The /dentifier can be configured on any
REA entity that needs to be identifiable, including the groups. As not all
REA entities are parts of some group, the Identifier Setup is often omitted
from the model, or is implicit in a software application, for example, as a
system table.


<!-- Page 170 -->


### 5.1 Identification Pattern 161

ee aoe
Setup
grouping Identification

=]
Fig. 111. Identification aspect in the application model
Design of the Identification Pattern
The aspect type level encapsulates the business logic of the aspect and conf
iguration parameters, which can be set by application developers. At the
aspect type level, the Identification Type defines the Name of the type of
identification, as well as other attributes. AutoNumber is a Boolean funct
ion that can be set on or off to indicate whether the Identifier can be
automatically generated by the identification aspect or not; automatically
generated number is often referred to as a number series. Unique is a Bool
ean function that can be set on or off to indicate whether or not the Identif
ier is required to be unique at runtime. Mandatory is a Boolean function
that can be set on or off to indicate if the Identifier must be defined at runt
ime or can be undefined.

The Identification Type Aspect has two elements, Identifier Type and
Identifier Setup Type. These elements contain business logic for interpreti
ng the ID rules, and logic for creating and validating Identifiers. They do
not have any configuration parameters; just serve as metadata for the Ident
ifier and the Identifier Type at the application level.

The rules for creating new Identifiers can vary from simple series with
linear increments to rules that allow for validity checks of the identificat
ion strings, such as credit card numbers. Legislation in some countries req
uires that numbers of some business documents consecutive, without
gaps, which imposes an extra requirement on how the number is cons
tructed. If an REA entity has been created by omission and deleted after
another REA entity of the same series has been created, the JD Rule must
be able to identify the gap in the series and reuse the number of the deleted
document.

The application model level specifies the runtime attributes that can be
set by the users of the business application, or automatically. At the appli-


<!-- Page 171 -->


162 5 Patterns

cation model level, the /dentifier element is configured on the REA entity
that should have some form of identity. The Jdentifier contains the ID
String, which provides an identity to each REA entity instance.

The Identifier Setup is usually configured on a group of REA entities
that share the same ID rule for creating or validating an Identifier. The JD
Rule determines how the identification strings are created (users of busin
ess applications often use combinations of letters and numbers). The JD
Rule can also be used for validating the identification strings entered
manually by the users of the business application. If the Identification Type
aspect is an AutoNumber, the Identifier Type also has an attribute Last ID,
which defines the last used identification string in the series.

Aspect Type Indentification

Aspect
Name
AutoNumber (Y/N)
Unique (Y/N)
Mandatory (Y/N)
1." 1%
Identifier Identifier
Type Setup Type
Application Model cinstancedfy _«instanceOf
' Group D> —-~------nnnn enone one
1 A
0..*
Fig. 112. Design of the identification pattern


<!-- Page 172 -->


### 5.1 Identification Pattern 163

Examples
The Social Security Number (SSN) of an employee is an identification that
is not an auto-number, is unique, and is not mandatory. The Identifier
Setup has the name SSN Numbering Scheme, and contains an ID Rule that
determines how the social security number is constructed or verified. The
Identifier has the name Social Security Number, and its ID String at runt
ime contains the social security number.
«Identifier Setup»
«group» SSN Numbering Scheme
«ldentification»
Social Security Number
«grouping» Mandatory = no
Unique = yes
AutoNumber = no
«Identifier»
«economic agent» Social Security Number
Fig. 113. Social Security Number
Sales Order Number is an identification that is an auto-number, is unique,
and is mandatory. As the Sales Order Number is an auto-number, the [dent
ifier Setup element contains the attribute Last ID.
«ldentifier Setup»
SO Number Series Setup
«group»
Sales Orders ID Rule
Last ID
«identification»
SO Number Series
«grouping» Mandatory = yes
Unique = yes
AutoNumber = yes
«ldentifier»
«contract» SO Number
Sales Order ID String
Fig. 114. Sales order number


<!-- Page 173 -->


164 5 Patterns

Product Serial Number is an identification that is an auto-number, is
unique, and is mandatory. The configuration of Product Serial Number is
similar to that of Sales Order Number in Fig. 114; Identifier is configured
on the economic resource Product, and Identifier Setup is configured on a
group of Products that belong to the same series.

Employee Name is an identification that is not an auto-number, is not
unique, but is mandatory. First name, middle name, last name and nickn
ame share the same ID Rule specified by Employee Name Setup.

«group»
Person
«ldentifier Setup»
Employee Name Setup
ID Rule
«ldentification»
«grouping» Employee Name
Mandatory = yes
- Unique = no
«economic agent» AutoNumber = no
Employee
«ldentifier»
First Name
ID String
|
«ldentifier»
Middle Name
ID String
|
«ldentifier»
Last Name
ID String
|
«ldentifier»
Nick Name
ID String
Po
Fig. 115. Employee name
Resulting Context
Sometimes, users of business applications use phone number, e-mail add
ress, or Internet address as identifiers of their trading partners. These
numbers and addresses have multiple and different semantics. Phone num-


<!-- Page 174 -->


### 5.1 Identification Pattern 165

ber can also be used as a contact address, e-mail address as a contact add
ress and destination location (for sending electronic documents and produ
cts), and Internet address as a description of the trading partner. In such
cases, different aspects will contain or refer to the same data (both identific
ation and notification will contain or refer to the same phone number).

There are several international standards specifying Identification
Strings and ID rules for economic resources and economic agents in vario
us lines of business. Examples are European Article Numbering (EAN)
for industrial products, International Standard Book Number (ISBN) for
books, International Standard Serial Number (ISSN) for periodicals, and
International Standard Music Number (ISMN) for printed music publicat
ions. For companies, the Data Universal Numbering System (DUNS) is
used. References to these standards can be found, for example, in (Arlow,
Neustadt 2003).


<!-- Page 175 -->


166 5 Patterns


### 5.2 Classification Pattern


Classification of a washing machine into an energy consumption class
Context

Users of software applications often need to divide REA entities, such as
economic resources, into certain categories. The example illustrated above
shows a classification of washing machines into categories A-G according
to energy consumption.

Such classification is essentially grouping, already described as a part of
GROUP PATTERN in Part I of this book. The reason we describe classific
ation as a structural pattern is that classification adds specific functionali
ty to the grouping structure. There are other patterns that add different
functionality to the grouping structure, for example BUDGET and
INVENTORY (not included in this book).

Problem

How do we model the hierarchy of classification categories, and classify

REA entities into the categories of the classification hierarchy?

Forces

Application designers have to consider the following forces:

e There is often a hierarchy of categories, and a REA entity can be classif
ied in more than one category simultaneously. For example, if a classi-


<!-- Page 176 -->


### 5.2 Classification Pattern 167

fication hierarchy for furniture contains category sofas with two subc
ategories, leather sofas and sleeping sofas, there can be an economic
resource (sofa), which can be both a sleeping sofa and a leather sofa.

e Often, two or more REA entities can use the same classification hierarc
hy. For example, a software support engineer can be classified accordi
ng his qualification as a Microsoft Windows supporter, or, more spec
ifically, a Windows 2000 supporter or Windows XP supporter. Thus,
the supporter can be classified using the same classification hierarchy as
is used to classify the software products.

e Sometimes, it is necessary to match REA entities that are classified in
the same categories. For example, the users of a business application
need to identify a supporter whose qualification corresponds to a produ
ct category.

e In some cases, users of business applications can classify an REA entity
themselves, in other cases the category depends on the values of some
attributes of the REA entity. In such cases, the REA entity should det
ermine its category automatically, and change it if the values of the att
ributes change. For example, a customer might be classified as a pref
erred customer if the volume of sales to him reaches a certain level, or
as an ordinary customer if the sales volume drops below that level.

Solution

The classification aspect in the application model has two elements. The

Member element on an REA entity classifies the REA entity into a Cate-

gory. The Category element defines a node in the classification hierarchy.

The Category element is usually configured on a Group entity, and the

Member element can be configured on any REA entity; see Fig. 116. The

Category has a reference to a parent Category, and thereby describes a hi-

erarchical structure; see Fig. 117.

An REA entity can be a member of several categories simultaneously.

These categories can be part of a single hierarchy, but also of different

classification hierarchies.


<!-- Page 177 -->


168 5 Patterns

=

Classification grouping
=
|

Fig. 116. Classification pattern
Design of the Classification Pattern
The aspect type level encapsulates the business logic of the aspect and conf
iguration parameters, which can be set by application developers. At the
aspect type level, the Classification Aspect defines the Name of the classif
ication hierarchy. The Auto-Classification is a Boolean parameter that can
be set on or off to indicate whether or not the classification aspect will
maintain the classification automatically based on the Membership Rule of
the Auto-Category. The classification Aspect has two elements, Category
Type and Member Type. The Member Type element has a Multiple Select
parameter that can be set on and off to indicate whether the REA entities at
the application level with an instance of this Member can be classified into
several categories or only into one. The Category Type element defines a
Name of the node type in the classification hierarchy.

The application model level specifies the runtime attributes that can be
set by the users of a business application or automatically by the business
logic. At the application model level, the Category represents a node in the
hierarchy of categories. The Category is usually contained in the Group
entity. Each node has a reference to a parent node, and thereby describes a
hierarchy of categories. A Category has the attribute Name, which specif
ies the name of the category. If the category is an Auto-Category, the
Membership Rule specifies the rule that enables the classification aspect to
create links between a Member and a Category dynamically, based on the
values of the Discriminator attribute of the Member. For example, if the
Discriminator of a Member is Age, the category with ‘Name = Minors’ has
the membership rule “Age is less than 18 years.”

A Member element can be configured on any REA entity, and has two
methods. The Js (Category) method allows the business logic to ask at run-


<!-- Page 178 -->


### 5.2 Classification Pattern 169

time if the REA entity with this member is classified as a specific Categ
ory. The method IsIn (Category) indicates whether the member is classif
ied in a subcategory of a specific Category. If the member is an AutoC
ategory member, the Discriminator attribute is used by the Membership
Rule to automatically determine the Category of the member.

Aspect Type
Name
AutoClassification (Y/N)
.
0..* ”
MultipleSelect (Y/N)
cinstanceOf»
Application Model parent category
«instanceOf» 0..*
Group ie
0..* /\
REA Entity 1 15 Category ) ;
A AutoCategory
Member
Fig. 117. Design of the classification pattern


<!-- Page 179 -->


170 5 Patterns
Examples
The Tax Group of an economic resource is a classification that is not an
Auto-Classification, because users of business applications explicitly speci
fy a tax group for each product type individually, according to local tax
regulations. The tax group classification does not have hierarchy (its hiera
rchy has only one level). The Name attribute of the Category element
contains the name of the tax category. For example, in Denmark, the categ
ories are “No tax,” and “25% Tax.” The Member element does not have
properties that can be set at runtime, and application developer does not
specify the Name of the Member Type.
___ JA has no name
«Classification»
grouping Tax Group
Auto-Classification = no
Esa pow |
Product Type «Member» ... || has no name and
member element has no
properties
Fig. 118. Tax group

Customer Group is a classification of the customers according to their
line of business. The configuration is similar to the configuration shown in
Fig. 118. The Member element is configured on the economic agent Cust
omer, and the Category is configured on the Customer Group. The categ
ories of the Customer Group can be “Agriculture,” “Insurance,” “Mini
ng,” “Educational Institution,” etc.

Qualification is a classification of employees according to their skills.
Qualification is usually not an Auto-Classification, and is hierarchical.

Age Classification is a classification of customers according to their age;
see Fig. 119. The Age Category is an Auto-Classification. For example, if
a customer’s age is less than 18 years, the customer belongs to the ‘min
ors’ category; customers over 18 years belong to the ‘adults’ category.
The Discriminator attribute of the Member is a reference to the State prope
rty of the DUE DATE PATTERN Age; the membership rule of the ‘min
ors’ category is “Discriminator == Upcoming’; the membership rule of
the ‘adults’ category is ‘Discriminator == Expired’. A customer’s age
changes over time, and if it becomes 18 years, the Due Date Age changes


<!-- Page 180 -->


### 5.2 Classification Pattern 171

its state from Upcoming to Expired, and consequently, the Classification
aspect changes the customer’s category from the minors category to the
adults category.

«group»
Age Group
«Category»
Age Group
Membership Rule
ee ie
«Classification»
«grouping» Age Classification
Auto-Classification = yes
«economic agent»
Customer
«Member»
Age Group Member
«dependsOn» Age
\ Date
i Duration
“~~Ib> State
Po
Fig. 119. Age classification
Runtime snapshot of the age classification example is shown in
Fig. 120. There are two instances of the Age Group entity; one has an ins
tance of the Category aspect with the name ‘minor,’ and the other has it
with the name ‘adult’, with corresponding membership rules. The Cust
omer has configured two aspects. The Due Date aspect determines the
age of customer as higher than 18 years. The Member element of the Age
Classification aspect contains a reference to the State property of the Due
Date aspect. The value of State determines that a link (an instance of the
grouping relationship) exists between customer and the ‘Adult’ age group.


<!-- Page 181 -->


172 5 Patterns
«group» «group»
: Age Group : Age Group
«Category» «Category»
Age Group Age Group
Name = ”Minor” Name = Adult”
MembershipRule="Discriminator==Upcoming” MembershipRule="Discriminator==Expired”
«grouping»
«Classification»
; Age Classification
Auto-Classification = yes
Customer
«Member»
Age Group Member
[re
«Due Date»
Age
Date = 2 February 1994
Duration = 18 Years
State = Expired
Cd
Fig. 120. A snapshot of the age classification at runtime
Resulting Context
The Age Category is often used for specifying policies that originate in leg
al regulations.

Although the REA entities belong to different categories, they may
share the same business logic. In many situations, it is what applications
designers intend. However, if the instances of REA entities that belong to
different categories should also have very different business logic, a better
solution would be to model them as different REA entities, rather than as
one entity with a classification aspect.

The design described in Solution in Detail enables users of business app
lications to create new categories in existing classification hierarchies,
and classify REA entities into these categories. However, only application
designers (not end users) can create new kinds of classification hierarchies.
This is suitable for most applications we came across, because a new class
ification hierarchy in a sense changes the application design. In the cases
where the users of business applications require the creating of new classi-


<!-- Page 182 -->


### 5.2 Classification Pattern 173

fication hierarchies at runtime, the solution must either be modified by
adding the Classification Type aspect element at the application model
level, or give the users of business applications application development
rights.

If a hierarchy of categories is needed in a business application, creating
a hierarchy is a nontrivial task, and creating a classification system usually
needs the help of a specialist.

Categories are often used to specify policies (see the POLICY
PATTERN). Categories enable users to introduce more business knowledge
into a business application, which has both benefits and drawbacks. The
drawback is that the business knowledge in the software system is somet
imes hard to create and needs maintenance as the business changes. The
benefit is that a software application that is aware of the business knowle
dge can more efficiently guide and help its users.


<!-- Page 183 -->


174 5 Patterns

### 5.3 Location Pattern

: ket ap
eT yale S22 Sn,
Location is a point in space
Context
Most economic events take place in time and space. For some economic
events, the location is an essential attribute characterizing them. Shipment,
for example, is an economic event in which an economic resource is
moved from one location to another. Users of business application are int
erested not only in departure and destination, but often also in the actual
location of the economic resource during the economic event.

Problem

How do we specify where the economic events occur?

Forces

We need to balance the following forces when creating the model:

e Economic resources that are physical in nature are usually located at
specific places in the world. Users of business applications would like to
know where a resource is.

e Information modeled as an REA economic resource also has location.
Information is always stored on a medium that has a location, and inf
ormation can be transferred from medium to another.

e Economic events contain historical information about changes of feat
ures of economic resources or transfers of rights to these resources.
These changes and transfers occur both in time and space.


<!-- Page 184 -->


### 5.3 Location Pattern 175


e Economic resources can change their locations as a result of economic

events or by forces outside of the scope of the application model. If an

economic event changes the location of the resource, users of business

applications would like to plan, monitor, and control changes of loca-

tions of the resources.
Solution
In the REA application model, the Location is an aspect consisting of two
elements, Position and Route, see Fig. 121. The Position element specifies
the actual position, and the Route element that represents the changes of
the Position. The Position element is usually configured on an economic
resource; and the Route element can be configured on a commitment,
which specifies the indented route, or on an economic event, which specif
ies the actual route.

reservation
Economic or stockflow Commitment or
Resource Ls Economic Event
Lo Lo

Fig. 121. Location pattern
Design of the Location Pattern
The aspect type level encapsulates the business logic of the aspect and conf
iguration parameters, which can be set by application developers. At the
aspect type level, the Location Aspect defines the Name of the location asp
ect. The location aspect has two element types, the Position Type and the
Route Type, both having properties defining their names. The Route Type
has a method DisplayMap() that displays the actual route and navigation
instructions.

The application model level specifies the runtime properties of the asp
ect elements. At the application model level, the Position element has an
attribute Actual Position which contains the actual position of the resource.
The Route element specifies a route segment that represents a change in


<!-- Page 185 -->


176 5 Patterns
the resource location. The Route element contains the properties Origin,
Destination, and Distance.
Aspect Type
«instance of» instance of»

Application Model

Commitment or 0..*

‘ Economic Event re Origin .

' ! ' Destination

OO ! Distance

0..*
1

‘ Resource oo
Fig. 122. Design of location pattern
Examples
The example in Fig. 123 illustrates a model of the location pattern configu
red as a Shipment Address. The route segment element is configured on
the Shipment economic event; the Destination property represents the final
address. The Position element is configured on the Item; the Location
property represents the actual location of the Item.


<!-- Page 186 -->


### 5.3 Location Pattern 177

Shipment Destination Name = Shipment Route
Distance
Location

sipckiion Name = Shipment Address
«economic resource» : Position
Name = Item Location

Fig. 123. Shipment address

The location pattern can be used to model an itinerary that consists of
several route segments. The itinerary is essentially a schedule. There are
several ways to construct the REA model for an itinerary, depending on
the level of detail of information the users of business application would
like to plan, monitor, and control. We will present one possible design.
The whole route is represented as an increment commitment, Transport,
and its Route aspect element contains the origin and final destination of the
economic resource Cargo. The decrement commitment, Cargo on Carrier,
represents the time interval on which the Cargo is loaded onto a specific
carrier; it is a decrement commitment because when Cargo is on a vehicle,
its possible use for other purposes is limited. The commitment Cargo on
Vehicle has a Route aspect element called Segment, representing a segment
of the scheduled transport. At runtime, there can be several instances of the
Cargo on Carrier commitment, for example, if cargo is transported using
several vehicles or means of transportation.

The other decrement commitment, Vehicle Use, has also a Route aspect
element. The Origin and Destination of the Vehicle Use element and the
Origin and Destination of the Cargo on Carrier element can be different,
for example, if a vehicle drives unloaded to the loading destination, and
then transports Cargo and, again, drives back unloaded. The cost of using
unloaded vehicle should be reflected in the cost of the transport, which is
what the model does.

An itinerary usually also contains information about time, such as when
cargo has been loaded, unloaded, and reached the final destination. This
can be modeled using the DUE DATE PATTERN on commitments and the
POSTING PATTERN on economic events.


<!-- Page 187 -->


178 5 Patterns
The location pattern can also be configured on economic events instead
of commitments; the business application would then monitor the actual
movement of Cargo.
«schedule»
Itinerary
Vehicle
«clause» «clause»
«Position» «clause»
Vehicle Location
«Location» [
Vehicle Itinerary
«use»
«increment «conversion» «decrement
commitment» commitment»
Transport <> Vehicle Use
Route»
5 Origin
Destination ;
; Distance
Distance a
Po
«produce»
«Location»
«economic resource» Cargo Itinerary «decrement
Cargo commitment»
—— «use» Cargo on Carrier
«Route»
Item Location
. Origin
Distance
| ee
Fig. 124. Itinerary
Resulting Context
How do we determine the shipping address of the customer? Some busin
ess applications store a shipping address (as well as the billing address,
and many other addresses) as attributes of a customer. These addresses are
then used as default addresses for shipments, invoices, etc. Users of busi-


<!-- Page 188 -->


### 5.3 Location Pattern 179

ness applications have the option to overwrite these addresses in case a
customer wishes to use a different address than his default address.

The solution in this pattern does not have a fixed default customer add
ress. Economic events contain all relevant historical information about
business relationship between the enterprise and the customer, and comm
itments that the enterprise gave to the customer. The economic events
and commitments also specify the shipping, billing, and other addresses
the customer has used in the past. The address for the next shipment can
then be determined by browsing the list of economic events. The customer
may then choose one of the existing destinations, a new one, or one that
the business application can suggest, for example, the destination of the
last shipment, as a default address. This solution is more flexible than that
of a fixed default address as a property of the customer entity because it
develops automatically as the enterprise’s information about the customer
develops.


<!-- Page 189 -->


180 5 Patterns

### 5.4 Posting Pattern


i

| oe

The posting behavioral pattern keeps track of transactions and makes their
records immutable
Context
Registering the history of the realized or intended exchanges of economic
resources is an important part of the functionality of most business softw
are solutions. For example, when economic agents agree upon a comm
itment, the commitment should be registered, and all modifications to the
registration should be traceable. Tax authorities often set similar requirem
ents for exchange economic events and for materialized claims; any
changes made to the data that influence the economic results of the comp
any should be traceable.

The history of business relationships is typically related to realized or
intended exchanges of economic resources, contracts, agreements, and
claims, such as the purchase and sale of products and services, invoices,
and corresponding payments.

Problem

How do we keep track of the history of economic events, commitments,
contracts, and other entities that represent interactions between economic
agents?

Forces

The solution is influenced by two forces:


<!-- Page 190 -->


### 5.4 Posting Pattern 181


e Users of business applications would like to retrieve comprehensive
analytical information about economic events, and commitments in their
business applications, and to perform data mining on huge amounts of
data related to their businesses. This is the purpose of the ACCOUNT
PATTERN; however, the ACCOUNT PATTERN requires that the busin
ess application already contains the data describing each transaction.

e There are often legal requirements on traceability of data that affect the
financial status of an enterprise. For example, if the users of business
applications made an error when entering the financial data into the syst
em, the original (erroneous) information should often not just be del
eted or overwritten, but a new record that eliminates the effect of the err
or should be made.

Solution

The main purpose of the Posting pattern is to keep a history of the eco-

nomic events, commitments, contracts, and claims. The posting pattern at

the application level consists of two elements; see Fig. 125. The Entry
element registers the value of the REA entity (for example, it stores it in
the database), together with the values specified by the Dimension elem
ent, which provides additional information about the event. After the ent
ry has been registered, the values of the entry and dimension are immutab
le; no changes to these values are possible.
Also entities indirectly IN
related via other entities
Any REA Entity any relation Commitment,
Economic
Event, Contract
|__| Le |

Fig. 125. Posting

Design of the Posting Pattern

The aspect type level encapsulates the business logic of the aspect and con-

figuration parameters, which can be set by application developers. At the


<!-- Page 191 -->


182 5 Patterns

aspect type level, the Posting Aspect defines the possible sets of comparab
le entries known to the business application. The possible sets are specif
ied by the Name property, and can, for example, be inventory posting, fin
ance posting, man-hours posting, or distance posting. The Posting Aspect
has a property, Unit of Measure Type, describing which Units of Measure
are allowed for the actual entries. A Unit of Measure Type can, for examp
le, be Cash. This would allow actual entries whose Unit of Measure is
USD, GBP, or EUR.

The Entry Type contains information about actual entries. The Value
Rule attribute contains information about how the actual entries retrieve
the value for the entry. The Value Rule is usually a reference to another asp
ect on the same REA entity.

The Dimension Type represents descriptive information that users of a
business application register with each entry. Examples of dimensions are
data characterizing an economic resource, resource type, economic agent,
and agent type related to the REA entity with the Entry element. The data
stored in the dimension is later used by the ACCOUNT pattern to construct
comprehensive reports and to retrieve statistical information. The Value
Rule property contains information about how the actual entries retrieve
the value for each dimension. The Value Rule is usually a reference to
other aspects on the same REA entity as the one on which Dimension is
configured.

The application model level specifies the aspect elements with run-time
properties. At the application model level, the aspect consists of one Entry
and several Dimension elements. The responsibility of the Entry element is
to enable keeping track of the history of the entities with the entry element.
The Value and Unit of Measure attributes represent the numerical value
registered with the Entry. The method commit() saves the data specified
by the Entry and the related Dimensions. The method commit() also makes
immutable the data referenced by the Value Rule of the Entry. That is, no
changes in these attributes are allowed after this operation has successfully
finished. If the entry contained erroneous information, the only way to
undo its effect to create and commit another entry that eliminates the effect
of the error. Date Occurred contains the time interval or date on which the
registered event or commitment occurred (it can actually be a start date
and an end date), while When Noticed contains the date on which the ent
ries were registered.


<!-- Page 192 -->


### 5.4 Posting Pattern 183

st
Name
Unit of Measure Type
= aa "
Dimension
:
0..* 1
Application Model cinstanceOf»
«instanceOf»
! Value
Docccceseennnncccccccececect i Unit of Measure
' Date Occurred
Date Registered
0..*
Resource oo
Fig. 126. Design of posting pattern
The Dimension element describes the information to be registered with
each entry. This is typically values of aspects on REA entities related to
the entity with the entry element. If the entry is configured on an economic
event, the dimensions applicable would be economic agent, economic res
ource, agent types and groups, and resource types and groups. The Value
attribute contains the information specified by the Value Rule of the Dim
ension at the Aspect Type level.
Example
Fig. 127 illustrates an example of Financial Posting. The economic event
Cash Receipt is related to economic resource Cash and economic agent
Customer.


<!-- Page 193 -->


184 5 Patterns
«economic agent»
Customer «participation»
Name «increment event»
esa Fee
; 5 «Entry»
«Dimension» .
Value Rule = Name.!DString «Posting» .
Financial Posting Value
Unit of Measure Type = Cash Date Occured
a Date Registered
«resource type»
Cash Le
. «Value»
Cash
|
«ldentifier»
Check Number

|
Fig. 127. Example of Cash Receipt with Posting and Dimensions
The increment Cash Receipt contains the Entry element (Posting aspect)
and the Amount element (Value aspect, see the VALUE PATTERN). The
Value Rule of the Entry element is configured to retrieve data from
Amount. Customer and Cash have both a Dimension element (Posting asp
ect) and an Identification element. The Value Rule of the both Dimension
elements is configured to retrieve the value of the ID String from the ident
ification aspects.

If the method commit() on the Entry aspect is called, the values of the
Entry aspect and the Amount aspect on Cash Receipt are locked, and the
data including the values of the dimensions is stored.

Resulting Context

The solution above allows the application designers to choose which dim
ensions will be registered with each entry type. By default, it is useful to
assume that the dimensions are all data on the REA entities directly related
to the entity with the entry aspect, plus groups and types of these entities.
However, users of business applications might point exactly to the data in


<!-- Page 194 -->


### 5.4 Posting Pattern 185

which they are interested. This level of freedom is useful especially if the
application model is not a full REA model but a modeling compromise.


<!-- Page 195 -->


186 5 Patterns


### 5.5 Account Pattern


oe

Ks

———

In some cases, keeping track of individual entities is not feasible or even
possible. For instance, once wine has been poured into a glass, it is no
longer possible to distinguish between the wine that was in the glass bef
ore the wine was poured into it and the wine that come from the bottle.
The only thing that it is possible to keep track of is the total amount of
wine in the glass.

The total amount of wine in the glass is the difference between the
amount of wine added in it and the amount of wine removed from it. Howe
ver, sometimes it is not precisely this difference. Wine might be poured
out of evaporate over time. Wine might also be added to the glass for reas
ons beyond the scope of the model. There might also be some amount of
wine in the glass before we start registering the additions and removals.

It is not feasible to model every possible way in which how wine can be
added to or removed from the glass, because many of them are not relevant
from the perspective and for the purpose of our model. A model is, by
definition, incomplete compared to reality. The only amount we know prec
isely is that of the wine in the glass at the time we measured it.

Context

The POSTING PATTERN describes how to record the history of economic
events, commitments, contracts, and claims. However, merely keeping
track of these entities is usually not the main interest of an enterprise’s dec
ision makers. Users of business applications are also interested in aggreg
ated information about the state of the enterprise.


<!-- Page 196 -->


### 5.5 Account Pattern 187


Problem

How do we keep track of the total amount of something being increased or

decreased, and eventually compute what constitutes the total amount?

Forces

To address this problem, the following forces must be resolved:

e Users of business applications would like to retrieve aggregated inform
ation about economic events, and commitments in their business app
lications, and to perform data mining on huge amounts of data related
to their businesses.

e Application designers could manually write an algorithm that retrieves
the aggregated information from the database; however, they would
rather like to anticipate from the application design what information the
users of this application will be interested in.

e Business logic should be triggered if certain values reach a certain level.
For example, if the inventory goes below a certain limit, a reorder
should be planned. If the balance of a bank account goes above a certain
threshold, the interest rate should change.

e Users of a business application would like to be informed about what a
total amount could be if an economic event occurs. For example, during
decrement or increment commitment, users of the business application
would like to know the amount of available resources at the promised or
expected time.

Solution

Whenever the value of some REA entity is a sum of the values of related

entities, an REA account aspect pattern is applicable.

At the application level, the REA account retrieves the values of all Add
ition and Subtraction elements on the related economic events or comm
itments. The balance of the account is the difference between the sum of
the Additions on the economic events or commitments and the sum of the
Subtractions on the economic events or commitments see Fig. 128, unless
the balance has changed for reasons outside the scope of the model.

The account pattern usually retrieves the values of the Addition and Subt
raction as well as the supplementary information from the Entry element
of the POSTING PATTERN. Therefore, the ACCOUNT and POSTING pat-


<!-- Page 197 -->


188 5 Patterns
tern are typically used together. The values of the Dimensions registered
by the POSTING pattern are available to the ACCOUNT pattern; therefore,
users of business applications can perform an analysis of the aggregated
value provided by the ACCOUNT pattern.
Agent, Resource
inflow, outflow, produce, use, consume, or Group inflow, outflow, produce, use, consume,
reservation, provide or receive reservation, provide or receive
Economic Event Economic Event
or Commitment or Commitment
= al
[ |
Fig. 128. Account in the REA application model
Design of the Account Pattern
The aspect type level encapsulates the business logic of the aspect and conf
iguration parameters, which can be set by application developers. At the
aspect type level, the Account Aspect defines the possible sets of comparab
le entries known to the business application. The possible sets are specif
ied in the Name attribute and can, for example, be inventory account, fin
ance account, man-hour account, or distance account. The Account Type
has an attribute, Unit of Measure Type, describing which Units of Measure
are allowed for the accounts. A Unit of Measure Type can, for example, be
Money. This would allow the Accounts to be measured in USD, GBP, or
EUR.

The Addition Type and Subtraction Type contain information about the
values that increase and decrease the balance of the account. The Value
Rule attributes contain information about how the addition and subtraction
elements retrieve their values. The Value Rule is usually a reference to the
Entry element of the Posting aspect. In this way, the posting dimensions
are also available to the Account, which are then used in the Perform
Analysis() method of the Account.


<!-- Page 198 -->


### 5.5 Account Pattern 189

Aspect Type
Dimension
Name Type
Unit of Measure Type
Type Name
Name Unit of Measure
Value Rule 0..* Max Balance
Min Balance
! 1
' Subtraction RecalculateValue()
| Type :
Name 0..*
Value Rule 0..* '
«instanceOf»
«instanceOf» Level Rule
OnRaiseAbove
OnSinkBelow
Application Model
| AgentorGroup | ' '
Poo ' Balance
| ! 1 Unit of Measure
1 1 Perform Analysis( )
penne] Addition
' H 0..*
4 ' One
pence eee Nene e eee ' Registered
a : * Balance
to Date Time
Fig. 129. Design of the account pattern
The RecalculateValue() method is called whenever the Balance of the
Account should be recalculated. Calling this method will update account
Balance based on the values of the Additions and Subtractions and the last
Registered Balance. The Balance can be updated whenever there is a new
instance of an economic event or commitment with the Addition or Sub-


<!-- Page 199 -->


190 5 Patterns

traction. However, some implementation technologies, such as OLAP
(Online Analytical Processing), do not allow updating as frequently as at
every transaction, in which case the RecalculateValue() method is called
whenever applicable. Max Value is the highest possible balance. Min
Value is the lowest possible balance. Trying to go beyond these limits will
cause Recalculate Value() to return an error.

Account Level assigns special actions to specific Balance levels of the
account that can be calculated using the Level Rule. Whenever the Recalc
ulate Value causes the account Balance to increase from a value on or bel
ow a level to a value above it, the event referenced in the OnRaiseAbove
property will be invoked. Similarly, decreasing the balance from on or
above the level to below it will cause the event referenced in the OnSinkB
elow property to be invoked.

Dimension Type describes the additional information associated with the
Account Type. The dimensions represent the things for which statistical inf
ormation can be obtained. The actual dimension values are provided by
the POSTING pattern, and the Account Dimension Type specifies which
posting dimension type will be used for analytical processing in the acc
ount aspect.

The application model level specifies the runtime attributes which can
be set by the users of a business application or automatically by the syst
em. At the application model level, Account contains the actual aggreg
ated values. The Balance and UnitOfMeasure properties contain the agg
regated value. The PerformAnalysis() method lets the user of a business
application drill down according to the dimensions from the associated Ent
ries of the POSTING PATTERN, and to perform analysis on the account
balance based on these dimensions.

The Registered Balance element contains the Balance and the Date
Time when the balance was measured. The Registered Balance can be
used, for example, to set the initial balance of the account, and to provide a
way to set the account balance if it changes for reasons beyond the scope
of the application model.

Examples
Fig. 130 illustrates an example of an account on economic resource Cash.


<!-- Page 200 -->


### 5.5 Account Pattern 191

«economic agent» “provides
Customer
«Dimension» «Posting» «increment event»
Customer Financial Posting Cash Receipt
ValueRule=Name.|IDString
«Entry»
Cashentry
|
«ldentifier»
Name Pe
F «Addition»
IDString Add Cash
TT Value Rule=CashEntry.Value
|
Cash F
— «inflow»
«Account»
Cash Account «Account» «exchange»
UnitOfMeasure = USD Financial Account
UnitOfMesureType = Money
[SY «decrement event»
«outflow» Cash Disbursement
«receive» «Subtraction»
SubtractCash
Value Rule=CashEntry.Value
Vendor
- |
«ldentifier»
CashEntry
— eS
«Dimension» «Posting» es
Vendor Financial Posting
ValueRule=Name.|DString
Po
Fig. 130. A cash account
This account is part of the aspect called Financial Account. The name of
the account is Cash Account, and its addition element is called Add Cash,
configured on the Cash Receipt economic event. This addition element rec
eives a value from Entry called Cash Entry. The subtraction element of
the account is configured on the Cash Disbursement economic event, and


<!-- Page 201 -->


192 5 Patterns
is called Subtract Cash. The Value Rule specifies that the subtraction elem
ent receives a value from the Entry called Cash Entry.

The entry Cash Entry configured on Customer has one dimension, Cust
omer. The entry Cash Entry configured on Vendor has one dimension,
Vendor. The two dimensions can become available to the account aspect.
Therefore, the users of a business application can perform an analysis of
the Cash Account balance, and display partial balances for each Customer
and each Vendor.

«reservation» «decrement commitment»
Sales Order Line
Inventory Account
Po
«fulfillment»
«outflow»
«decrement event»
Sale
«resource group»
Goods To Sell
el
en
aAcsounty Ee
Goods On Stock
«Account»
Purchase
Lee
«Account»
Inventory Account

Po

«inflow»

«fulfillment»
«reservation» «increment commitment»
Purchase Order Line
Lo
Fig. 131. Inventory accounts


<!-- Page 202 -->


### 5.5 Account Pattern 193


Fig. 131 shows an example of three inventory accounts. An inventory is
a group of items on hand. The account Goods to Sell has increment on
commitment Sales Order Line, and decrement on economic event Sale.
The account Goods on Stock has increment on economic event Purchase,
and decrement on economic event Sale. The account Goods to Receive has
increment on commitment Purchase Order Line, and decrement on econ
omic event Purchase. Observe that there is no rule that the addition asp
ect element must be on an increment economic event, or vice versa. For
example, the subtraction element of the Goods To Sell account is configu
red on the increment economic event Purchase.

Resulting Context

As described earlier, the REA model differs from double entry accounting
because it considers economic transactions (economic events and comm
itments) as primary data about the enterprise, and aggregated informat
ion such as accounts, are information derived from the economic transact
ions; or, in other words, as reports.

The account in double entry accounting is different than the REA acc
ount. In double entry, every account has two sides, the debit or left side,
and the credit or right side. Double entry accounting means that every
transaction (economic event or commitment) is recorded twice: once on
the debit side and once on the credit side. An entry is called balanced if the
sum of the debit amounts equals to the sum of the credit amounts.

Double entry accounting is a common practice in many companies for
keeping track of financial data, and is required by legislation in some
countries.

We can derive all information that is needed for constructing accounts
in double entry accounting from the REA model. Therefore, if local legisl
ation or tax authorities require a financial report as it would be in the doub
le entry system, it can be created on demand from the REA model. Howe
ver, the users of a business application can still keep the richness of the
information provided by the REA model for their management decisions.


<!-- Page 203 -->


194 5 Patterns

### 5.6 Materialized Claim Pattern

Invoice
Hotel: Y Date: 11 February 2005
Guest: X
Service Price
semement
Claim
Outflow and inflow economic events usually do not occur simultaneously,
and the exchange duality between the economic events is out of balance
for a certain period of time. In this case, it is common practice for one
economic agent to send another an invoice to settle the amount owed
Context
When a company receives goods or services from its vendor, it often does
not pay for them until it receives an invoice. The invoice basically says:
”We gave you the goods; give us the money, please.”

A contract between the company and its vendor usually specifies the
payment terms, so the company could in fact pay for the goods according
to the payment terms. If the unbalanced amount is known to both econ
omic agents, the invoice is not necessary; it is just common business
practice. However, in some cases, the price is not known when the contract
is signed. For example, a service technician who provides maintenance of
equipment is paid according to material and time consumption. In this
case, the invoice also specifies the value of the economic event, which corr
esponds to the time and material used to perform the maintenance.
Problem
How do we keep the economic agents informed about unbalanced econ
omic exchanges?


<!-- Page 204 -->


### 5.6 Materialized Claim Pattern 195


Forces

Three forces must be considered when solving this problem:

e If all economic agents involved can keep their models synchronized, the
materialized claim would not be necessary. However, this is not always
the case.

e Legal reasons might require a document specifying the unbalanced
value. For example, value-added tax (VAT) in some countries is calcul
ated as a percentage of the invoiced amount, and the document specifyi
ng this amount must be made available to tax authorities.

e The exact unbalanced value between the inflow and outflow events
might be unknown to one of the economic agents. For example, a serv
ice contract might specify payments according to consumption. When
the service is finished, the consumption is known to the service provider
and to the customer. Or a vendor sometimes adds a shipping fee to the
price of products, whose exact value might not be known to the purc
haser at the time of purchase. We want to ensure that economic agents
agree upon the unbalanced value.

Solution

Whenever an economic event occurs without the occurrence of all corre-

sponding dual economic events, there exists a claim between economic

agents related to the economic events.
cl =a
es a
tec os
Materialized
Claim
|

Fig. 132. Materialized claim
The Materialized Claim is a physical representation of an REA entity

Claim. A responsibility of a Claim is to contain the unbalanced value and

business logic determining whether there is an imbalance. The Materiali-


<!-- Page 205 -->


196 5 Patterns

zation and Settlement elements contain information about the unbalanced
economic events.

Design of the Materialized Claim Pattern

The structure of the Materialized Claim pattern is shown in Fig. 133. The
aspect type level encapsulates the business logic of the aspect and configur
ation parameters, which can be set by application developers. At the asp
ect type level, the Claim Type contains the Name of the materialized
claim, such as Invoice or Credit Memo. The Materialization Type and Sett
lement Type contain Value Rule, Resource Rule, and Agent Rule that det
ermine how the Value, Resource, and Agent properties of Materialization
are obtained.

The application model level specifies the runtime attributes that can be
set by the users of business applications or automatically by the system. At
the application model level, the Materialization element contains the Value
of an economic event that is used to materialize, ie., create, the claim. As
economic events can contain several value attributes, such as quantity,
price, and cost, the materialization element specifies which of these values
will be considered as an input for the balance. This Value increases the
Unbalanced Value of the Materialized Claim. The Resource and Agent att
ributes contain relevant information about the economic resource and
agent related to the economic event. What information is considered relev
ant depends on the requirements of the users of the business application,
but it usually is an identification of the resource and agent.

The Settlement element contains the Value of the economic event that is
used to settle the claim. This value decreases the Unbalanced Value of the
Materialized Claim. The Economic Resource and Economic Agent attribu
tes contain relevant information about the economic resource and agent
related to the economic event, usually their identifications.

The Materialized Claim element is a report that contains information
about the unbalanced value and relevant information about economic
events related by exchange duality. The Unbalanced Value can be obtained
as the difference between the values of the Materialization and Settlement
elements. The Date Time attribute specifies when the attributes of the Mat
erialized Claim have been made valid, for example, when an invoice has
been created.


<!-- Page 206 -->


### 5.6 Materialized Claim Pattern 197

Aspect Type
Materialized Claim Aspect
0..*
0..*
Materialization
Type 1+ Claim Type
Resource Rule
Agent Rule '
«instanceOf» Type «instanceOf»
Value Rule
Resource Rule
Agent Rule
Application Model «instanceOf»
0." i
Claim cciumnatan Leinianammammamael eam Claim
L eeccconeeeeeeeeneed : Unbalanced Value
: 0.. 1 Unit of Measure
t ' 1 DateTime
0." | Value 0..*
Unit of Measure
\ Agent
a Value
' . ! 0.* Unit of Measure
' Economic Event > ------------------".---| Resource
or Commitment Agent
Fig. 133. Design of the materialized claim pattern
Examples
The Invoice is a materialized claim: the customer should pay for the goods
or services the vendor provided.
When a vendor ships goods to the customer, a claim exists until the cust
omer pays for the goods. At any time after the shipment, the vendor can


<!-- Page 207 -->


198 5 Patterns
materialize this claim, i.e., create an invoice. The Materialization and Sett
lement elements correspond to the invoice lines in Fig. 134. When the
customer eventually pays for the goods, each invoice line will be related
via settlement relationship to the payment.
0.* «exchange» 0.*
Accomodation Payment
| =
a SS
:
Invoice Materialized
Claim
l=
Fig. 134. A materialized claim

Credit Memo is a materialized claim: the customer overpaid the vendor.
A Credit Memo is usually accompanied with a payment that settles the
claim, but it does not have to be. For example, partners can agree to deduct
the credit amount in the following payment.

An interesting situation occurs in the cases where the economic agents
related to the increment event are different from the agents related to the
decrement event. If a library receives a donation from a sponsor to lend
books to readers, a claim between the sponsor and the library exists unless
the library provides the lending service to readers; see Fig. 135. What to do
with the money from the donation the library has not spent can be determ
ined by the terms of the donation, which is the contract between the
sponsor and the library.


<!-- Page 208 -->


### 5.6 Materialized Claim Pattern 199

«economic agent» «economic agent» «economic agent»
Reader Library Sponsor
«receive» «provide». «receive» «provide»
«outflow» «decrement event» | «exchange» «increment event» «inflow»
Lend Donation Receipt
«economic resource» «economic resource»
Book Cash
Report
To: Sponsor Date: 31 December 2003
From: Library
Service Amount
Materialization Lending Books -10.650,00
Settlement Donation 25.000,00
Claim Balance 14.350,00
Fig. 135. Donations create claims
Resulting Context
In the REA modeling framework, a materialized claim such as an invoice
is a kind of report, containing information derived from economic events.
This contrasts with some business software applications, where the invoice
is the central part of the business solution. Invoices and other materialized
claims are needed when the business management tools are pen and paper,
but as the data in business applications of trading partners can be kept sync
hronized, invoices are not necessary in order to run a business.

A materialized claim can contain information about all unbalanced dua
lities between participating economic agents. The invoice created accordi
ng to the model in

Fig. 134 is not limited to a single order, but can contain claims from all
shipped but unpaid for orders for a specific economic agent. Many compan
ies have a business practice to create one invoice per sales order, but this


<!-- Page 209 -->


200 5 Patterns

is probably due to the limitations of their software business solutions,
rather than their business needs. But if this is a user requirement, the patt
ern can be easily restricted to materialize only subsets of the claim limited
to specific contracts, simply by adding a relationship between the contract
and the materialized claim.

The claim contains information about aggregated unbalanced amounts,
but does not answer the question about which decrement and increment
events match, such as which received payments are for which sales. The
RECONCILIATION PATTERN answers this question.


<!-- Page 210 -->


### 5.7 Reconciliation Pattern 201


### 5.7 Reconciliation Pattern

Sales $ 20, Jan 6, 2003 Payment $ 25, Jan 10, 2003
sO
aay ae Sa $ 45, 20 Jan 2003
Sales $ 25, Jan 10, 2003 ae
a rte ‘ .
ay CX cee
oad <——___—_ >
Sales $ 50, Jan 25, 2003 Payment $ 5, 30 Jan 2003
Have you ever experienced a situation in which a company has received a
payment, it was difficult for it to determine what this payment is for?
Context
One of the REA domain rules specify that in the REA application model,
every increment event must be related to a decrement event, and vice
versa. This rule must also be applied at runtime; each actual instance of an
increment event must eventually be related to one or more actual instances
of a decrement event, and vice versa.
Problem
How do users of business applications find which occurrences of increm
ent and decrement economic events should match?
Forces
The following forces must be resolved in the solution:


<!-- Page 211 -->


202 5 Patterns

e Some inflow economic events, such as payments, come with incomplete
information about who sent cash, and what the payment was for. Users
of business application would like to match this payment with one or
more of the outflow events, such as sales.

e Sometimes, the received payment does not exactly match the price of
the sold goods or services, and sometimes a payment comes in several
installments. Users of business applications would like to match the
payments with sales, and to determine the outstanding balance of a
given customer.

e Sometimes, economic events do not exactly match the commitments.
Users of business applications would like to determine which economic
events match which commitments.

e Sometimes, the matching amounts are not exactly the same, but the diff
erence is so small that it is not worth of claiming it. These situations
can happen, for example, due to changing exchange rates when dealing
with different currencies. Users of a business application might like to
have the possibility to declare these events as matching, even when the
numbers differ.

Solution

The Reconciliation pattern is essentially a many-to-many relationship be-

tween increment and decrement economic events related by the exchange

or conversion duality, or between commitments and economic events rel
ated by fulfillment, or between commitments related by reciprocity. The

Initiator and Terminator elements hold the values to be reconciled.

exchange, conversion,
reciprocity, fulfillment
Economic Event Economic Event
== al
Lo [Lo

Fig. 136. A reconciliation

Design of the Reconciliation Pattern

The reconciliation pattern at the aspect type level and the application

model level is illustrated in Fig. 137.


<!-- Page 212 -->


### 5.7 Reconciliation Pattern 203

Aspect Type
Name

### 1.4 Reconciliation Method

tg: Terminator
Initiator T
Name 1.* 1." Name
Value Rule - _ Value Rule
ID Rule ID Rule
Application Model cinstanceOt» <instanceOf»
Commitment or ~ 0..*
Economic Event ' oe ' eee
Ld . Value
: 0.. Unbalanced Value
0." Reconciled ID [ ]
i _ IsReconciled
Commitment or 0..*
' . @ -------------| Value
; Economic Event | Unbalanced Value
eee ence ened Reconciled ID [ ]
IsReconciled

Fig. 137. Design of the reconciliation pattern

The aspect type level encapsulates the business logic of the aspect and

configuration parameters that can be set by application developers. At the

aspect type level, the Reconciliation Aspect contains an attribute for its

Name and a Reconciliation Method. The reconciliation method is an enu-

meration that can be set by the application developer, and determines the

strategy of how to match the initiator and terminator values. Some of the
reconciliation strategies are:

e The values of oldest initiator and terminator are matched first. If these
values are not the same, the difference is applied to the next oldest init
iator or terminator, and the difference is matched to the next oldest init
iator or terminator. The outstanding value is stored in the Unbalanced
Value attribute of the newest terminator or initiator element.


<!-- Page 213 -->


204 5 Patterns

e The values that are the same or most similar are matched first. If there
are values that cannot be matched, the difference is stored in the Unbala
nced Value attribute of the terminator or initiator that differs most from
the corresponding terminator or initiator.

e The matching economic event is determined from supplementary inform
ation provided by the events. For example, if the payment contains a
shipment number, then the payment and shipment are matched.

e Manually, users of business applications can themselves find the matchi
ng values, and determine which of the implemented methods should be
used, (for example, oldest first).

The Initiator Type and Terminator Type contain the Value Rule attribute
that determines how the Initiator and Terminator obtain their values. The
Reconciliation Pattern in the application model must have at least one Init
iator and at least one Terminator element.

At the application model level, which specifies the runtime attributes
that can be set by the users of a business application or automatically by
the system, the reconciliation consists of two elements. The Jnitiator and
Terminator elements have the attributes of Value, holding the value to be
reconciled, and Unbalanced Value, holding the value that has not been
reconciled. By setting a Boolean value IsReconciled, a user can declare the
Initiator and Terminator as reconciled even if they have a nonzero unbala
nced value. The Initiator and Terminator elements are usually configured
on commitments and economic events related by duality, reciprocity, or
fulfillment relationships.

Examples

Fig. 138 illustrates how a reconciliation can be applied between a Sale and

the corresponding Cash Receipt.


<!-- Page 214 -->


### 5.7 Reconciliation Pattern 205

|
Sale Cash Receipt
Sale Price «Reconciliation» Payment
Cash Sale

Value Value

Reconciled ID [ ] Reconciled ID [ ]

IsReconciled IsReconciled
Fig. 138. Reconciliation between commitment and economic event

An enterprise made three sales to a customer: S001 for USD20, S002
for USD25, and S003 for USD50; it received three payments from the cust
omer: P0O1 for USD 25, P002 for USD45, and P003 for 5 USD. The
problem is to match sales and payments.

Table 2 below, shows the Initiator and Terminator in a table format. We
decide to match the same or similar values (see also the illustration before
the Context section) first. We match SOO] with P002, and the payment
P002 will cover the shipments SOOJ and S003. As the customer has not
paid enough to cover the three sales, sale SOO3 has an unbalanced value of
USD 20.

Table 2. Example of reconciliation
Initiator
Event ID Value Unbalanced Reconciled
Value ID
S001 20 0 PO02
S002 25 0 PO01
$003 50 20 P002, P003
Terminator
Event ID Value Unbalanced Reconciled
Value ID
POO1 25 0 S002
PO02 45 0 $001, S003
PO003 5 0 S003
Resulting Context
In order to use the reconciliation pattern, the values of the initiator and
terminator must be comparable; they must have the same or transformable
units of measure. For example, we cannot directly compare quantity in


<!-- Page 215 -->


206 5 Patterns

pieces and price in USD, so if a shipment specifies quantity in pieces but
not prices in USD, we cannot use the reconciliation pattern to find the
matching payment. If the commitment specifies monetary value in one curr
ency, but the enterprise receives payment in the different currency, busin
ess application must have some functionality for comparing these values
in order match the unbalanced value. Some unit conversions, but not all,
are handled by the VALUE PATTERN.

The unbalanced value of the initiator or terminator elements can be used
as an unbalanced value of the materialized claim. It will be a positive or a
negative claim, depending on whether the initiator and terminator will be
configured on an increment or an decrement economic event, respectively.


<!-- Page 216 -->


### 5.8 Due Date Pattern 207


### 5.8 Due Date Pattern


we
a" ‘7 “
4 —— >
4,

Due date is the time by which something must be finished or completed

Context

Deadlines, starting dates, renewal dates, and last payment dates are exam-

ples of the dates that are often of high importance to users of business ap-

plications. Often certain actions have to be taken, and things have to be
done on or before these dates, or within a certain time period after these
dates.

Problem

How to model due dates in the REA model, and how can a business appli-

cation help users to manage the dates?

Forces

If you deal with due dates, you many need to address the following forces:

e Due dates specify moments that occur in the future. It usually does not
make sense to set a due date for an event that has occurred in the past.

e The due dates are usually properties of commitments, claims, and cont
racts. Some commitments specify time intervals, such as the commitm
ents in conversion processes; some specify instantaneous events, such
as those for change of ownership.

e Some time events are often related to other time events; for example,
users of business applications might like to receive a customer’s paym
ent within 30 days from the invoice date.


<!-- Page 217 -->


208 5 Patterns
Solution
We will illustrate a simple version of the pattern that satisfies the forces; it
consists of one element, Due Date. Due Date is one of the patterns that do
not crosscut REA entities. We will discuss a more complex version of the
due date pattern in the resulting context section. The Due Date pattern can
be used whenever the business logic needs to specify the deadlines, miles
tones, and dates that will or should occur in the future, as well as the dep
endencies of these dates on other dates.
Commitment,

|_ see
Fig. 139. Due date pattern
Design of the Due Date Pattern
A design of the Due Date pattern is illustrated in Fig. 140. The due date
pattern at the aspect type level encapsulates the business logic of the due
date aspect and configuration parameters, which can be set by application
developers. The Due Date Type specifies the configuration parameters for
the Due Date elements. Editable is an enumeration indicating whether the
user can, cannot, or must edit the Date of the Due Date. The Activation
Rule specifies how the Date property of the Due Date is determined. The
Activation Rule specifies how the Date depends on other dates or other
values in a business application. Often, the due date pattern needs to have
knowledge of a calendar. The Activation Rule can specify, for example,
that payment should occur on the fifth day of the month, following a specif
ied date, unless the payment date is Saturday, Sunday, or a public holiday,
in which case the date is the preceding week day.


<!-- Page 218 -->


### 5.8 Due Date Pattern 209

Aspect Type
0..*
Activation Rule
Editable
Application Model
«instance of»

| REACommitment | 0..*

Contract, Term Date

oo Duration

State
Fig. 140. Design of the due date pattern

The application model level specifies the runtime attributes of the due
dates that can be set by the user of a business application or automatically
by the system. The State of the Due Date can be Upcoming, Expired, or
Disabled. Before the time specified by the Date, the State of the Due Date
is Upcoming. After the Date, the State is Expired. The State of the Due
Date can also be Disabled. The Date specifies the date and time the due
date expires. The Date can be editable by the user of a business applicat
ion, depending on the configuration property Editable on Due Date Type.
Many Due Dates expire after a period of time after the date specified by
the Activation Rule. The Duration specifies the difference between the
dates calculated by the Activation Rule and the Date. The users of business
applications may edit the Duration; therefore, it is a property of Due Date
and not Due Date Type.

Time intervals, for example, the duration of a task, can be modeled as
two due date aspect patterns. One Due Date specifies the start of the task.
Another Due Date specifies the end of the task. The activation rule of the
second due date is configured to receive the value of the first due date, and
the duration specifies the length of the task. An example is illustrated in
Fig. 143.


<!-- Page 219 -->


210 5 Patterns
Examples
The example in Fig. 141 illustrates the application model in which an Jnv
oice specifies that a payment has to be made within certain time interval
after the invoice date. This example also illustrates that a general concept
of date or time is contained in several aspects; each aspect specifies the
semantic of the date and time in its own context.
; Payment Line
«commitment»
Payment Deadline
Editable = ’can edit’
——_ Activation Rule = Invoice.DateOcurred
«fulfillment» Date
1 |
«fulfillment»
«decrement event» «exchange» \ «increment event»
Sale 1 Cash Receipt
«Entry» «Entry»
Sale Cash Receipt
_————s «claim» Po
Invoice |
«settlement»
«materialization»
Invoice |
Fig. 141. An invoice specifying payment

The moment at which the Invoice is issued is modeled as the Date Occ
urred property of the Entry element of the Posting aspect on the Invoice.
The Activation Rule of the Due Date element is configured to receive the
Date Occurred value, and the Duration specifies the delay.

The model in Fig. 142 shows an example of two due date aspects that
specify a time interval. The Start Task aspect has a blank activation rule
and the Date must be set by users of the business application. The End
Task aspect has an Activation Rule set to refer to the Date of the Start
Task; its Duration specifies the duration of the task; and users of the busin
ess application can edit the date.


<!-- Page 220 -->


### 5.8 Due Date Pattern 211

«commitment»
Task
«Due Date»
Start Task
Editable = ‘must edit’
Activation Rule ="’
Duration
_-}— Date
“
/ |
«dependsOn» «Due Date»
\ End Task
NC Editable = ‘can edit’
~~-f Activation Rule = StartTask.Date
Duration
Date
a |
Fig. 142. The start and end of a task
«commitment»
Task
«Due Date»
Start Task
Editable = ’can edit’
_7[ Activation Rule = EndTask.Date - EndTask.Duration
cr
/ / Duration
ig Date
adeperldsOn» np
i]
{ \ «Due Date»
vf End Task
«dependsOn» ‘\ Editable = ’must edit’
VA Activation Rule =’’
\ \
XO Duration
= Date
Fig. 143. The scheduled end and the duration of a task determines its scheduled
start
In the planning of conversion processes, often the scheduled end date of
an economic event is known, and the planners need to determine the latest
start date. The scheduled start and the scheduled end of the economic
event, specified by the commitments, are Due Dates. The activation rule of
the scheduled start due date is set to calculate it from the duration and the
scheduled end date, see Fig. 143.


<!-- Page 221 -->


212 5 Patterns

Resulting Context

The general concept of time is contained in several aspect patterns. The
POSTING PATTERN contains the dates when economic events, commitm
ents, and claims occurred and when they have were registered. The DUE
DATE PATTERN captures the information about when an event should occ
ur.

What if users of business applications would like to edit and create their
own activation rules? In other words, what if we add another force to this
pattern: “The rules specifying dependencies between dates should be edita
ble by the users of the business application.” Then, the activation rule
must be present as a property of some element on the application model;
for example, we must add a element Due Date Setup to the aspect at the
application model level. At the aspect type level, the Due Date Setup Type
will specify a language in which the users of the business application exp
ress the activation rules. For example, an activation rule ‘27D’ in a busin
ess software application Navision determines the due date as 21 days
from now, ‘CM+8D’ means that the due date is at the end of the current
month plus eight days. These rules can determine the dependency on the
current date, but not a dependency on other dates specified in the business
application.

The Due Dates are never configured on economic events, because econ
omic events register what has already happened, while the due dates repr
esent moments that will occur in the future.


<!-- Page 222 -->


### 5.9 Description Pattern 213


### 5.9 Description Pattern


= T-shirt with Miami Beach Topics
Yio Relax in this high quality (Hanes-Beefy-T)
white T-shirt with Miami Beach Topics silks
creened on the front. The back is plain.

A description of an item from a product catalogue

Context

REA entities, especially economic resources and resource types, contain

information about real things. This information is presented to users of

business applications in many different ways and formats. Some of the inf
ormation also comes in unstructured form.

Problem

How do we store unstructured information about REA entities?

Forces

The following forces need consideration:

e Products can be described in many different ways. For some entities,
simple text is sufficient, but a description can also be graphical. Des
criptions can also incorporate sound or other digital multimedia.

e Some forms of description are standardized or regulated by professional
bodies, such as various types of specifications and drawings.

e In addition to products, which are economic resources, users of business
applications often store unstructured information about other REA entit
ies, such as economic agents and events.


<!-- Page 223 -->


214 5 Patterns
Solution
Description aspect pattern can be used to store unstructured information
about REA entities. The sketch of a solution is illustrated in Fig. 144. Des
cription pattern does not crosscut other entities, and can be configured on
any REA entity.

==

|

Fig. 144. Description pattern
Design of the Description Pattern
The aspect type level encapsulates the business logic of the description asp
ect and the configuration parameters, which can be set by application dev
elopers. At the aspect type level, the Description Aspect Type defines the
Name of the type of description. The Media attribute of Description Type
determines what kind of information can be held by the Description elem
ent. Examples of Media can be text, multiline text, picture, or Web add
ress.

The application model level specifies the runtime attributes that can be
set by the users of business applications or automatically. At the applicat
ion model level, Description contains an attribute that at runtime contains
a description of the instance of the REA entity.

Textual Description remains the most flexible means of describing an
REA entity. Textual description is always in a specific language; for some
business solutions it is necessary to provide the textual information in seve
ral languages.

Graphical Description is often used in to describe products in product
catalogues, but can also be used to store drawings and diagrams.

Web Page is a pointer to the description of an REA entity on the Intern
et. A Web Page is often used as a description of economic agents such as
companies. The attribute Internet Address contains a URL (Universal Res
ource Locator), a text string pointing to the description of the business ob-


<!-- Page 224 -->


### 5.9 Description Pattern 215

ject on the Internet. The Web Page is often used as a description of the
economic agents.

Aspect Type
0..*
Description
Type
cinstanceOt»
Application Model
i REAEntity @>---------------------------"----)___ Description
Textual Graphical
Fig. 145. Design of the description pattern
Examples
An example of a food item Product Type is illustrated in Fig. 146. This
Product Type is an economic resource with configured description patterns
Picture, Product Description (which is supposed to contain textual des
cription of the product, in free text), and Cleaning Instructions (a textual
description), usually for both before and after opening the product.

The other example illustrated in Fig. 146 is a Customer VAT Group. At
runtime, users of a business application will classify the customers into
several VAT groups, and describe the Purpose of each group with free
text.


<!-- Page 225 -->


216 5 Patterns
tomer
Product Type Customer VAT
Group
«Description» —
Picture «Description»
Media = Text
—
«Description» Ld
Media = Text
«Description»
Media = Text
Lo
Fig. 146. Product type and customer group with description patterns
Resulting Context
The DESCRIPTION PATTERN is intended to store unstructured informat
ion about an REA entity. If an application developer would like to store
structured information, it should use other patterns. Thinking about struct
ure of the descriptions is often a way to discover new behavioral patterns.

IDENTIFICATION PATTERN is related to DESCRIPTION PATTERN.
Although a Description can be also used to identify an entity, it is not its
primary purpose. Usually, it is better to have one or more dedicated Identif
iers using the IDENTIFICATION PATTERN.

NOTE PATTERN is also related to DESCRIPTION PATTERN. Both
Note and Description store unstructured information. The difference is that
the primary purpose of the Description is to store information that des
cribes an REA entity. The Note can be used to store any unstructured inf
ormation about the REA entity. It is also usual that different users of
business application will have different access rights to the Description
then to the Note. While a Description about the product can be made availa
ble to the customers, the Notes might contain internal information for
salesmen or warehouse personnel.


<!-- Page 226 -->


### 5.10 Notification Pattern 217


### 5.10 Notification Pattern


SMS (Short Message Service) is a text message to be sent and received to a

mobile phone via the network operator

Context

Various users of business applications should often be notified when cer-

tain events occur, or when certain conditions become true. For example,

both customer and bank personnel might be interested in being notified
when the customer account has been overdrawn. Business applications can
be configured to create and send notifications automatically.

Problem

How do we notify users of business applications about changes in the REA

entities?

Forces

Several forces arise when designing the solution:

e There are different ways to contact users of business applications. The
notification can range from a message box window on a computer
screen to sending a letter to a specified address.

e Different users of business applications can be contacted in different
ways. Some users can be contacted in multiple ways. The method of not
ification can vary, depending upon the user and upon the kind of notific
ation.

e Different users are interested in different information resulting from the
same change.


<!-- Page 227 -->


218 5 Patterns
Solution
Notification is a specific unit of functionality that encapsulates the mechan
ism for notifying users of business applications. A notification pattern
consists of two elements. The Address element contains the way to contact
the economic agent. The Message element contains the information forw
arded to the agent, as well as the logic determining when the agent is not
ified.
Any REA Entity Economic
Agent
Notification
a ee (a
| |
Fig. 147. Notification pattern
Design of Notification Pattern
At the aspect type level, Notification Type contains the Name of the notific
ation, and encapsulates the business logic of forwarding messages to spec
ific addresses. The Media Rule defines which Media the specific Address
is allowed to contain, and hence determines which attributes a specific Add
ress Type contains (for example, street, city, and zip code for postal add
ress), and consequently also which message types can be delivered to
which kinds of addresses; hence, the Media Rule. Examples of Media are
postal address, e-mail address, and SMS address.


<!-- Page 228 -->


### 5.10 Notification Pattern 219

Aspect Type
Notification Aspect
Name
Media Rule
Message Type Address Type
Message Rule A
Notify()
Application Model F
«instance of» «instance of»
Any REA Entity @------------------------ Message
System Postal E-Mail Voice SMS
Message Message Message Message Message
0..*
| Economic Agent Q>--------------------------nnnnn nnn nnn Address
System Postal E-Mail Voice SMS
Address Address Address Address Address
Lema |
US Address French Address etc
Name Name
Street Street
City Locality
State CEDEX postcode and area indicator
Zip Code
Fig. 148. Design of the notification pattern


<!-- Page 229 -->


220 5 Patterns

The Message Type element has the responsibility of creating a message.
Name specifies the name of the message type. The Message Rule attribute
specifies how the message will be created. The simplest approach is to use
a predefined message for each message type; a more complex approach is
to create a message at runtime by composing it from predefined informat
ion and relevant data available. When the Notify() method is called, the
message is created and the user notified.

At the application model level, Message can be configured on any REA
entity, and represents a message that can be sent to an Address. Message
can be one of the listed examples of messages (System, Postal, E-mail,
Voice, SMS, and so on). Address is usually configured on an economic
agent, and can be one of the listed examples of addresses. Each address
contains different elements and rules. The business logic at the aspect type
level determines which message types can be delivered on which address
types.

«economic agent»
Customer
«Account»
Bank Account
«Account level» ST \
«SMS Message» eae
ee Account Level Notification
Po
Fig. 149. Notification on account event
Examples

Fig. 149 shows an example of a Customer economic agent that is notif
ied when its Account level sinks below its credit limit. Customer is conf
igured with the Notification aspect, where both the Message and the Add
ress elements are configured at the Customer entity. The OnSinkBelow


<!-- Page 230 -->


### 5.10 Notification Pattern 221

event of the Account Level (a part of the Account Type element, see the
ACCOUNT PATTERN) causes the Notify() message of SMS Message elem
ent to send an SMS message with the Balance of the Bank Account asp
ect element as Text

A mobile phone operator, T-Mobile, in some countries sends a voice
and an SMS message to its customers every time a customer receives a
message in his voice mail. Fig. 150 shows how this functionality could be
implemented using the notification aspect pattern.

«group» «economic agent»

Voice Mailbox Customer
—— |
«Notification» «Address»

«economic resource»

Voice Mail

Message

Fig. 150. Notification on new voice mail

Voice Mail Messages are economic resources that are members of the
group Voice Mailbox of a specific customer. Whenever someone records a
new voice mail message, the grouping relationship calls a Notify() method
of the Voice Notification and SMS Notification elements. These elements
create the voice and text messages and send them to the customer Phone
Number.


<!-- Page 231 -->


222 5 Patterns

### 5.11 Note Pattern

» 7 ‘ ;

— / = ~, >

- . ‘ gy

The postman would like to remember his experience with various custom-

ers, and perhaps share it with other postmen

Context

Users of business applications often require from them the possibility to

add various comments and remarks to various entities. These remarks are

not a description of the entity; rather, they contain information such as

their experience with the customer, promises salesmen gave to customers

that are too indefinite to become commitments, and similar remarks.

Problem

How do we record unstructured and ad hoc information about REA enti-

ties?

Forces

The following forces arise:

e The remarks and comments are unstructured and are often written as
plain text.

e The stored information is often intended only for internal use in the
company.

e An REA entity can have many remarks and comments attached.

e Sometimes it is useful to store the date and author with each remark to
keep track of the development of the entity.


<!-- Page 232 -->


### 5.11 Note Pattern 223

Solution
The note aspect pattern can be used to attach comments, observations, and
notes to REA entities. The Note aspect pattern is illustrated in Fig. 151. It
consists of two elements, the Note element, whose responsibility is to rec
ord the comment, and the Author element, which identifies who wrote the
note.
Any REA Entity Economic
Agent
a ees cel
| |
Fig. 151. Note aspect pattern
Design of the Note Pattern
The structure of the Note pattern is illustrated in Fig. 152. At the aspect
type level, the Note Type specifies its Name of the Note Type, as users of
business applications might want to attach different types of notes to REA
entities. Author Type specifies the ID Rule that determines the information
that will identify the author of the Note.

At the application model level, Note represents one or more comments
on an REA entity. Note contains the Text of the note, and the Date when
the text was written. The Author contains the Author ID attribute that ident
ifies the author. There can be multiple instances of the Note of the same
Note Type on a single instance of an REA entity.


<!-- Page 233 -->


224 5 Patterns
Aspect Type
0..* 0..*
Author
i
[Name | [Name [0-1
ID Rule
Application Model
«instanceOf» —_«instanceOf»
i Economic Agent @>---------------------} ----}
0."
{ Any REA Entity +@>-------------
io Date
Fig. 152. Design of the note pattern
Examples
The example in Fig. 153 illustrates two note aspects, Promise and Experie
nce, configured on economic agents Salesman and Customer. Fig. 154
shows a runtime snapshot of this Note; observe that there can be several
instances of Notes of the same Note Aspect, such as several Promises.


<!-- Page 234 -->


### 5.11 Note Pattern 225

«economic agent» «economic agent»
Customer Salesman
Text Promise
ed Po
Text
ee
Lo
Fig. 153. A configured note aspect pattern
«economic agent» «economic agent»
i Customer : Salesman
Text = Special Price AuthorID = Tom
Date = 20 May 2004
«Note»
PT Promise
Text = Rush Delivery
Date = 21 April 2004
PT
Text= Responds to E «noe
~ xperience =
Sales letters P AuthorID = Addy
Date = 17 Dec 2004 DoT
Lo
Fig. 154. The note aspect pattern at runtime
Resulting Context
The NOTE PATTERN is related to the DESCRIPTION PATTERN in the
sense that both can store unstructured information. However, the purpose
of description and note is different. While the purpose of description is to
store the information that actually describes the REA entity, the note can
be used to store any unstructured information about an REA entity.
Another difference is that while at runtime there is usually only one Des
cription instance per configured Description Aspect in an REA entity,
there can be multiple instances of Note.


<!-- Page 235 -->


226 5 Patterns

### 5.12 Value Pattern


i . |
The value of an object is often measured in money, but the value is influe
nced by many factors. For example, carat weight, clarity, color, and cut
contribute to the value of a diamond
Context
A basic assumption for why a rational enterprise has exactly the business
processes it has, is that these business processes add value to the resources
that are under the control of the enterprise. During exchange processes,
economic agents receive resources of higher value than those they give up;
in conversion processes the value of produced resources is higher than the
resources used and consumed.

In practice, this qualitative answer is often not sufficient. Users of busin
ess applications would like quantitative information about how much
value each instance of the process adds.

Problem

How do we represent quantitative information about the value of the REA

entities?

Forces

Resolving this problem effectively requires resolution of the following

forces:

e Although rational business processes add value, this is only true on ave
rage. Specific instances of value-adding processes can decrease the


<!-- Page 236 -->


### 5.12 Value Pattern 227

value of an enterprise’ resources®. Detailed information about the proce
sses is crucial for process improvement.

e As the value added by business processes is measured through the ent
repreneurial purpose of each process, it can be represented in various
units; production processes can be measured in terms of quantities and
exchange processes can be measured in terms of monetary values.

e Users of business applications might require that value be represented in
different units on demand. For example, if an enterprise issues an inv
oice in one currency and receives payment in another currency, there
must be some method how to estimate whether the values of invoice and
payment correspond.

e Sometimes, the value must be made immutable; for example, if an ent
erprise makes an offer to the customer (an offer is a suggested cont
ract), the price must not change, even if the values of the price elements
(such as material, tools, and services) change.

Solution

Value pattern holds information about the value of the REA entities. Val-

ues include prices, costs, quantities, taxes, discounts, and bonuses. A value

pattern is sketched in Fig. 155. Value is calculated from several Value

Components; for example, the value of tax can be calculated from the sales

price and the tax percentage. Both Value and Value Component are repre-

sented as a number and a unit. Values and Value Components can have diff
erent units; it is the responsibility of the Value Aspect to perform any conv
ersion.
conor ———]|_voue
Component
EE EE

Fig. 155. Value pattern

6 It has been reported that in the film industry, only about 10% of all produced
movies are profitable. On average, these 10% must generate enough profit to
cover the losses from the 90% of non profitable movies.


<!-- Page 237 -->


228 5 Patterns

Design of the Value Pattern

The aspect type level encapsulates the business logic of the aspect and conf
iguration parameters, which can be set by application developers. At the
aspect type level, the Value Aspect contains the Name of the aspect and
Calculation Rule, which is an expression of how the Value is calculated
from the Value Components. The Value Type holds the Name, and the Unit
of Measure. Further the Value has an operation, LockValue(), which locks
the value in the application model. The Value Component Type contains
the Name of the Value Component and the Unit of Measure. The Source
Rule defines how the value of the element is obtained, and usually refers to
values of other aspects. The Multiple property determines whether there
can be multiple elements of the same Value Component Type in the applic
ation model.

The Unit of Measure holds the Name and the Symbol of the Unit of
Measure that is used in the Value Component and the Value. The Convers
ion contains the Conversion Factor between various Units of Measure.
Some conversion factors, such as currency exchange rates, can be obtained
dynamically, for example, as Web services.

The application model level specifies the runtime attributes that can be
set by the users of business applications or automatically by the system. At
the application model level, the Value element holds the property Value
together with the Unit of Measure. The Value element is connected to zero
or more Value Components from which the Value is calculated. The Value
Component contains the Value with the Unit of Measure; therefore, it is
always possible to determine what Value Components the Value consists
of. The Value Components can be given in Units of Measure different from
that of the Value property of the Value element.


<!-- Page 238 -->


### 5.12 Value Pattern 229

Aspect Type
: Name
Calculation Rule
0..* 0..*
1 1
2
1
Name Value Component
Symbol Type Value Type
1 Name ; Name
0..* | Unit of Measure 0.. 1 | Unit of Measure
Source Rule Lock Value()
Application Model instance of» «instance of»
| Any REA entity Q>---------------— | -----n2-nnnanna a Value
ocecsceeenceennceceesnceeel Value
1 | Unit of Measure
0..*
i Any REA entity ne Value
foo Unit of Measure
Fig. 156. Design of the value pattern
Examples
The example in Fig. 157 illustrates the value aspect called Nominal Price,
consisting of one Value element and two Value Component elements. The
element Price is configured on the Sales Line and is calculated from two
elements, Quantity and Unit Price, simply by multiplying quantity with
unit price. When a contract is signed, the Lock Value() of the Price and
Quantity is invoked. Then, Price and Quantity do not change, even if the
Unit Price changes. Please note that users of business applications can
change the Unit of Measure of all three elements at runtime; in such a case,
the business logic will recalculate the Value.


<!-- Page 239 -->


230 5 Patterns
«economic resource»
Item «ouflow»
«Value Component»
Unik nies: «commitment»
Unit of Measure = USD per unit Sales Line
Value
Unit of Measure «Value»
Tr Price
Unit of Measure = USD
Value
Unit of Measure
Value Aspect LT
Name = Nominal Price Value C
Calculation Rule = Quantity. Value * UnitPrice.Value «Value Component»
Quantity
Unit of Measure = unit
Value
Unit of Measure
Po
Fig. 157. The nominal price of an item as a configured value aspect pattern


<!-- Page 240 -->


### 5.13 Inventor’s Paradox Pattern 231


### 5.13 Inventor’s Paradox Pattern

How to extend a business application in a consistent manner?
Context
Structural patterns describe the REA modeling framework for business
systems. The REA concepts have not significantly changed during last ten
years; therefore, we do not expect any radical change in it in the near fut
ure. The REA modeling framework has survived the test of time and has
been successfully implemented in several business standards.

In contrast, behavioral patterns represent the functionality of the busin
ess applications that originate in user requirements. It is natural to expect
that users of business applications will require richer, more powerful, and
generally better software applications in the future. Therefore, it is likely
that any limited list of behavioral patterns does not meet all future req
uirements the users of a business application could possibly have. When
application designers implement business applications, they are forced to
discover new patterns originating from unexpected user requirements.
Problem
How do we discover a new behavioral business pattern?

Forces

A solution is influenced by the following forces:

e Users of your business application require functionality that is not cove
red by the behavioral patterns we know about.


<!-- Page 241 -->


232 5 Patterns

e Users of business applications sometimes require very specific features
that are not always good candidates for behavioral patterns. Behavioral
patterns are generalized and reusable units of business logic; therefore,
it usually requires substantial work to transform a specific user requirem
ent into a business pattern.

e We would like a general rule or guidelines to help us formulate new
business patterns from new user requirements.

Solution

The solution is known as Inventor’s Paradox, described by the mathemati-

cian George Polya (Polya 1982):

“A solution to a general problem is often simpler than a solution to a

specific problem.”

In summary, the Inventor’s Paradox is as follows:

e Solve a specific problem by solving a more general problem.

e The general problem paradoxically has simpler solution.

e But you have to invent an appropriate general problem which covers
your specific problem.

To apply the Inventor’s Paradox, application designers analyze the use
rs’ business problems and try to extract patterns that can be generalized.
Then, they solve this generalized problem as one or more behavioral patt
erns. Finally, they solve each specific problem by configuring the behavi
oral patterns in a software business application.

The guidelines above are general, and can be applied to solving probl
ems in any domain. In model-driven design for software in a specific dom
ain, the application developers must keep in mind the purpose of the dom
ain, and generalize the specific problems in a way that is consistent with
the domain. This sounds easy; but, based on our experience, it is not.

We formulated the following guidelines to help application designers
focus on generalizing specific problems in the scope of the business logic
domain.

7 Polya’s original formulation was “The more ambitious plan may have more
chances of success, provided it is not based on a mere pretension, but on some
vision of the things beyond those immediately present.” We use the formulation
by Karl J. Lieberherr (Lieberherr 1997).


<!-- Page 242 -->


### 5.13 Inventor’s Paradox Pattern 233

The behavioral patterns described in this book

e have business semantics,

e are large units of functionality,

e often crosscut the structural patterns.

These principles are described in more detail below.

Behavioral Patterns Have Business Semantics

“What business problem does this requirement solve?” is probably the

most fundamental question to ask when examining a new user require-

ment. Users often tend to ask for a low-level or computational functionali
ty, and it is up to the application designer to discover the real business
purpose behind this requirement. For example,

— Is a function that computes a sum of numerical values a good candidate
for a behavioral pattern in the business domain? Without domain-driven
modeling in mind, a designer might think that he can generalize this req
uirement into an arithmetic operation pattern to cover subtraction,
multiplication, and division as well. Would it be a good behavioral patt
ern? We need to discover why the users need to sum values. Do the use
rs need it for making an order total? Do the users need it for calculating
the stock value of the product? The arithmetic operation is probably not
a good candidate for a behavioral pattern in the business domain, but
contract total or account might be.

— Is a currency converter a good candidate for a behavioral pattern in the
business domain? We need to discover why the users need a currency
converter. If they need it for calculating the value of a payment in ano
ther currency, for calculating payment for international customers, and
for calculating an offered price of the product, then monetary value will
be a better candidate for a business pattern than a currency converter.

Behavioral Patterns Are Large Units of Functionality

If application designers develop a single business application for a specific

purpose, they probably do not care about reuse. If user requirements

change, the designers just change the application. However, if the applicat
ion designers are developing a framework that will be used to configure
several business applications in a product line, or to configure several very
different business applications, then they would like to identify the func-


<!-- Page 243 -->


234 5 Patterns

tionality that is most complex and difficult to implement. Then, they can
implement this functionality once in the reusable framework, and configu
re the actual software applications.

In such an environment, the more the complex and difficult functionali
ty is implemented in the framework, the easier the job becomes for the
application designers in configuring the actual business applications, and
the less the overall amount of work (framework development plus applicat
ion development).

Therefore, the more the larger, and most complex and most difficult
units of functionality is implemented as behavioral patterns, the easier the
job of the application designers becomes. They can then focus on unders
tanding and modeling users’ business problems, rather than on implem
enting them.

Behavioral Patterns Often Crosscut Structural Patterns
Behavioral patterns often crosscut structural patterns; therefore, if a user
requires new functionality or a new data field on an REA entity, this will
probably require some collaboration with data on other REA entities.

An example is address. In many business applications customer and
vendor entities have addresses, such as shipping address and billing add
ress. However, the addresses are also properties of the purchase order,
sales order, and invoice. Therefore, it is useful to think of an address as a
module having two elements: the default address on an economic agent,
and the actual address on an economic event.

The address pattern presented in this book even has different design, in
which the default address is dynamically derived from historical informat
ion specified by economic events. Nevertheless, in both cases the address
element crosscuts the entities that originate from the domain categories.


<!-- Page 244 -->


6 An Aspect-Based Example Application

By Christian Vibe Scheller


### 6.1 Setting up the Application Model


In this chapter I will describe how a simple application can be built using
aspects. While the example given is very simple, it will hopefully give an
idea about the possible complexity of the applications that can be created
using the methods described.

The examples described in this chapter are based on a very simple task
management system developed in C#. Using the system it should be possib
le to register tasks. If the task is not completed after a specific time period
(e.g., 20 days after the task registration) the system will send a reminder to
a specified e-mail address.

For the sake of completeness it should be noted that the tasks managed
by this application can actually be thought of as commitments in the REA
model. Since this chapter focuses on the use of aspect patterns, however,
this knowledge is not used in the examples.

What we want to do is to assemble the task management system from
aspects each encapsulating part of the business logic that makes up the syst
em. Using the aspects described in the previous chapters we could end up
with something like this:
public class Task {

public Identifier ID =
new Identifier("TaskIdSequence", 10000, 1000);
public Description Text = new Description()i
public DueDate Due = new DueDate(20);
public Notification Notify = new Notification()j;

In this example each aspect is defined as a class. The domain class itself
is composed of aspects. The metadata used to specify the behavior of each
aspect is simply specified as parameters to the aspect’s constructor.


<!-- Page 245 -->


236 6 An Aspect-Based Example Application

What we see is that the Task has an Identifier which automatically gene
rates a sequence number given a seed of 10000 and a step of 1000. In
other words, the first task is called ‘10000’, the next ‘11000’ and so on.
The string TaskIdSequence specified in the Jdentifier’s constructor is nece
ssary for implementation reasons because the Identifier class does not
know its context and is therefore not able to distinguish the ID of a task
from the ID of an employee, sales order, etc. By assigning a unique text
string to the identifier, it can use this text string to create different number
series for different classes.

An alternative to this solution would be to inform the Identifier of its
context:

ID.Context = this;

It is in many ways desirable, however, that the aspects should not know
their context. This is primarily due to their nature as cross-cutting conc
erns. Experience shows that if the context is not known by the aspect the
chances of creating a truly “reusable” aspect is greater.

The task also has a description, called Text. In the example the descript
ion is just a simple text string of arbitrary length. The description does not
require any specific metadata.

The DueDate describes the date on which the task must be completed or
else the system will send a reminder to the responsible person. The implem
entation of the DueDate aspect includes a simple activation rule that calc
ulates the activation date by adding 20 days to the current date.

Finally the Notification aspect is responsible for sending the reminder to
the responsible person. In this very simple example only the e-mail type of
notification is supported and the responsible person’s e-mail address is
simply assigned explicitly (e.g., through the user interface) to the notificat
ion aspect.

A small problem with the way the example is implemented is that some
of the metadata is specified directly as parameters to the aspects’ construct
ors. This makes it difficult for other components to gain access to the
metadata through reflection. We are in other words “hiding” part of our
domain model by hard coding it into the class. A somewhat better solution
would be to use .Net attributes to specify the metadata:
public class Task {

[Identifier.Definition(Seed = 10000, Step = 1000)]
public Identifier ID = new Identifier()?
}


<!-- Page 246 -->


### 6.2 Creating the Aspect Code 237

This is a very nice solution because it allows the metadata to be ret
rieved through reflection. It does however also make the aspect code more
complicated because there is no easy way for an aspect class to retrieve the
attributes set on a specific property or field. In order to get this to work it
would again be necessary for each aspect to know its context. Later in this
chapter a solution to this problem will be described.

### 6.2 Creating the Aspect Code

In the task management system described it would probably be overkill to
actually write the code for each aspect instead of just including it in the
domain class itself. The idea is however that these aspects can be reused
over and over again within the same application or even across applicat
ions. The aspects can be seen as the business logic equivalents to visual
basic controls.
Se) (celta) lS) (TT) TTT) scr
[al
Rett
[a f=) : Ezpemect Te
a] ©] Pere reel et See ee eee e ESF eRe See Se eee ess BackColor 2HOQCOOOISE
EES BS eee om . ° Cancel False =
—— [Command
=A ‘ie Command! + pall
_ : Dragicon {none}
io lS ; | DragMode 0- Manual
@D) 4 Enabled True
HO : : | FortBold True
1} Fortltalic False
Fort ame MS Sans Seuit
Lad ontSize 25
| Seater eae Sinaia fac
FortUnderline False v
Fig. 158. Visual Basic 3.0 development environment with visual basic controls
Visual Basic controls have become popular in the development commun
ity because it is extremely simple to create a Windows form by dragging
a number of controls onto the form. Often these controls will be very powe
rful grid controls with spreadsheet functionality, image controls with adv
anced imaging capabilities and so on. By choosing the right control for
the job the developer can minimize the amount of code he needs to write.


<!-- Page 247 -->


238 6 An Aspect-Based Example Application

Similarly the idea behind aspects is that it should be easy to assemble a
domain class from aspects. Each aspect should ideally contain much of the
code that the developer would otherwise have to write explicitly on the
domain class itself.

Obviously the aspect implementations given in the example are very
simple, but they could easily be extended. For instance the Identification
aspect could contain code that checked the ID for uniqueness; it could cont
ain different algorithms for generating IDs (GUIDs, Initials, specially
formatted IDs such as social security numbers, etc.) and it could contain
hashing algorithms for easy retrieval of objects based on their ID.


### 6.3 The Identification Aspect

Let’s start with the Identification aspect:

In order to keep the example code simple only a subset of the identificat
ion aspect’s functionality has been implemented, namely an Identification
aspect with AutoNumber, Unique and Mandatory implicitly set to yes and
only the NumberSeries rule implemented.

For the purpose of this example, this is what the Identification aspect
might look like:
public class Identifier {

public int Value;
private static Dictionary<string, int> LastValue =
new Dictionary<string,int>()i
public Identifier(string sequence, int seed, int step) {
if (LastValue.ContainsKey(sequence)) {
Value = LastValue[sequence] + step?
} else {
Value = seedi
}
LastValue[sequence] = Value;
}
}

### 6.4 The Due Date Aspect

The implementation of the Due Date aspect implements a simple activat
ion rule that adds a number of days to the current date. The implementat
ion does not deal with durations.
With these limitations, the Due Date aspect might look like this:


<!-- Page 248 -->


### 6.4 The Due Date Aspect 239

public class DueDate {
public enum States { Active, Due, Completed, Canceled }
public DateTime Date;
public States State = States.Active;
public event EventHandler Duei
public event EventHandler Completed;
public event EventHandler Canceledi
private static List<DueDate> DueDates = new List<DueDate>()i
public DueDate(int days) {
Date = DateTime.Now.AddDays (days) i
DueDates.Add(this) i
}
public static void Check(DateTime date) {
foreach (DueDate dueDate in DueDates) {
if (dueDate.Date < date &&
dueDate.State == States.Active) {
dueDate.State = States.Due;
if (dueDate.Due != null) {
dueDate.Due(dueDate, null);
}
}
}
}
public void Complete(object sender, EventArgs e) {
State = States.Completedi
if (Completed != null) {
Completed(this, null);
}
}
public void Cancel(object sender, EventArgs e) {
if (State == States.Active) {
State = States.Canceledi
if (Canceled != null) {
Canceled(this, null);
}
}
}
}

There are a few things to note about the Due Date aspect: First of all it
provides the three event handlers: Due, Complete and Canceled. These
event handlers get called by the Due Date aspect when its state changes to
Due, Completed and Canceled respectively.

The Due Date aspect also implements the state diagram illustrated in
Fig. 159.

The static Check() method is meant to be called from time to time to
check if any Due Date aspect has reached its due date without being comp
leted or cancelled. The Check() method will then raise the Due event and
change the Due Date aspect’s state to Due. The Check() method uses a


<!-- Page 249 -->


240 6 An Aspect-Based Example Application
static list called DueDates to keep track of all the DueDates that have been
created.
Check()
Complete()
Cancel()
Due Canceled
(we Completed Canceled
Completed
©
Fig. 159. State diagram of the due date aspect

### 6.5 The Notification Aspect

The implementation of the Notification aspect only supports the e-mail
type of notification. Furthermore the responsible person’s e-mail address is
simply assigned explicitly (e.g., through the user interface) to the notificat
ion aspect.
This is what the Notification aspect might look like:
public delegate string MessageHandler()i
public class Notification {
public string EMailAddress;
public event MessageHandler Message;
public void Notify(object sender, EventArgs e) {
if (Message == null) {
MessageBox.Show("Notification caused by " +
sender, EMailAddress) i
} else {
MessageBox.Show(Message(), EMailAddress) ;
}
}
}
}


<!-- Page 250 -->


### 6.6 The Description Aspect 241


The Notification aspect consists of a simple text string containing the
email address of the recipient and a method called “Notify” that causes the
Notification aspect to send a notification to the recipient.

The message handler called Message allows the developer to specify the
message that the Notification aspect should send to the recipient by providi
ng a delegate to the Notification aspect. The Notification aspect provides
a default message in case the developer has not specified a message deleg
ate.


### 6.6 The Description Aspect

The implementation of the Description aspect only supports textual des
criptions. The implementation of the Description aspect simply looks like
this:
public class Description {

public string Value;
}

### 6.7 Interchanging Events Between Aspects

The last thing we need to do in order to get our little task management syst
em to work is to link the Notification aspect to the DueDate aspect so that
notifications will be sent out when the due date is reached. This is done by
providing a delegate to the Notification aspect’s Notify method to the
DueDate’s Due event.
public class Task {

public Identifier ID =

new Identifier("TaskSequence", 10000, 1000);

public Description Text = new Description()i

public DueDate Due = new DueDate(20)i

public Notification Notify = new Notification();

public Task() {

Due.Due += new EventHandler(Notify.Notify) ji

}
}

Now everything is in place. The DueDate aspect will monitor the task
and if the task is not completed before the due date it will invoke its “Due”
event handler. The Notification aspect in turn will receive the “Due” event
and react by sending an e-mail message to the recipient.


<!-- Page 251 -->


242 6 An Aspect-Based Example Application

The message could either be the default message “Notification caused
by DueDate” or it could be a specific message drawing on the task descript
ion:
public class Task {

public Identifier ID =

new Identifier("TaskSequence", 10000, 1000);
public Description Text = new Description()i
public DueDate Due = new DueDate(20);
public Notification Notify = new Notification();
public Task() {

Due.Due += new EventHandler(Notify.Notify);

notify.Message += delegate {

return Text.Value + " is due";
) }
}

The use of event handlers as the means of communication creates a publ
isher/subscriber pattern making sure that the aspects are loosely coupled.
This is an important factor in making sure that the aspects are reusable bet
ween domain classes.

The type of interaction exemplified by the notification message, where
data is sent from one aspect to another, and in the case of the notification
message even reformatted, is probably the most complex part of the asp
ect-based development method. This is where the developer needs to act
ually write code on the application model-level itself rather than relying
on the aspects to do the work.

Again comparing aspects to Visual Basic controls, this is the equivalent
of writing Visual Basic code on a button’s event handler.


### 6.8 Constructing the User Interface


One of the main benefits of using aspects is that they are truly cross cutting
concerns. In our little model of a task management system we have used
C# code to describe the definition of a task, but the definition goes further
than that. Let us recapitulate: A task is defined by its aspects. In the case of
the example application tasks are defined as:


<!-- Page 252 -->


### 6.8 Constructing the User Interface 243

Task |_|
Class
T
(=| Fields
@ Deadline : DueDate
@ ID: Identifier
@ Notify : Notification
@ Text : Description
Fig. 160. Task class
We can use this definition to derive a number of artifacts: user interface,
storage model, documentation, etc. Every one of these artifacts can be seen
as a view of the domain model. By using the domain model we can easily
construct this rather crude user interface:
EM Task Cex)
ID
10000
Text
Pay the phone bill
Deadline
January 20.2006
Notify
eMail john@doe.com
Fig. 161. User interface of task aspect
The user interface is constructed by iterating through the task’s aspects
and letting each aspect contribute with its own part of the user interface.
This is done in the code below:
foreach (FieldInfo fieldInfo in typeof(Task) .GetFields() ) {
AspectControl control = null;
switch (fieldInfo.FieldType.Name) {
case "Description": {
control = new DescriptionControl()ji
breaki


<!-- Page 253 -->


244 6 An Aspect-Based Example Application
}
case "Identifier": {
control = new IdentifierControl()i
breaki
}
case "DueDate": {
control = new DueDateControl()i
breaki
}
case "Notification": {
control = new NotificationControl()ji
breaki
}
default: {
continue;
}
}
panell.Controls.Add(control) i
control.Dock = DockStyle.Top;
control .BringToFront()i
control.Initialize(fieldInfo.GetValue(obj), fieldInfo.Name) i

}

Each aspect has a corresponding user interface part (implemented as a
user control) that is added to the user interface at runtime.

One importing thing to note is that as the aspects get more elaborate and
encapsulate more and more of the business logic, the user interface comp
onents will also get more and more elaborate and become small “applicat
ions” in themselves rather than just a bunch of textboxes.

The picture below shows a typical screen from Microsoft Navision™.,
While Microsoft Navision™ does not use aspects explicitly it is obvious
that aspect patterns exist per se in the user interface:


<!-- Page 254 -->


### 6.9 A Model-Based Framework 245

IX IN IN
Location Identification Due date
i 2007 sstartetion 14: Sales Order fe (6 {kK
General Invoking ssa . = sin. E- Coneice é Customer Information
Sebtocutener No, ST OrderDate... 2... | 17-01-01 FA *Shipto Addresses (2)
Seto Contact No... . (CTooodae Gel” Document Date. . . oe oa 0)
Sel-to Customer Name. (Selangorian ted.” Requested foe 25-02-25 —
Selto Address . . . on asprive 2 Promised Delverygate . 2740805 Bl-to Customer
Selo Addess2 ff. rar x Eternal Document No. . * val. Cred 0
Sel-to State (ZIP i. OH” x 40148) ghrosonto... €.. crixz2g
Selo Contact... Wiadbighetbur Responsibity Cente yy BIRMINGHAM
No. of Archived Versions?” ty) \ Status ss.» Open
To NO 2 scription = v Quantty Reserve... Unit of M... Unit Phic.. 3 en
| gGOO2 / Cables for Loudspeakers “RUE 2 Box 24,00 MATERIALS + There Card 4
PF 1... 15-Sx5 Rend for Loudspeokers (5-150 SOK 12 Pcs 72,00 MATERIALS : Dp (-30)
PI... LSSANIO Manual for Coudepeakers BLUE x” PCS | MATERIALS Ps ©
( [tu 15-75 Loudspeatier, Cherry, 750 eid Q rs 79,00 MATERIALS ben aay ok
~ j \ | Une Di... (0)
Description Claim Classification
Fig. 162. Behavioral patterns in Microsoft Navision

### 6.9 A Model-Based Framework

Until now we have based our task management example on code; in this
case written in C#. In the following chapter I will describe an alternative
solution: Namely to specify the domain class in an XML document.
In our example we will create an XML document that looks like this:


<!-- Page 255 -->


246 6 An Aspect-Based Example Application
<Class name="Task" type="Commitment ">
<Aspects>
<Identifier name="ID" seed="10000" step="1000"/>
<Description name="Text" />
<DueDate name="Deadline" days="20"/>
<Notification name="Notify"/>
</Aspects>
<Subscriptions>
<Subscription source="Deadline" sourceevent="Due"
target="Notify" targetevent="Notify" />
</Subscriptions>
<Delegates>
<Delegate target="Notify" property="Message">
return Text.Value + " is due";
</Delegate>
</Delegates>
</Class>

The XML document contains a single Class tag with the attribute name
having the value Task. This tells the reader that the task management syst
em contains a single domain class with the name Task. The Class tag cont
ains three sections.

The first section is qualified with an Aspects tag. This section contains
the definitions of each of the aspects that the task consists of. In this case
we already know the aspects from the previous chapter, namely JD, Text,
Deadline and Notify. Metadata for each aspect is expressed as attributes to
the corresponding XML tag.

Note that the text string TaskIdSequence, which had to be included
when we used C# is no longer necessary when we use XML. The reason
for this is that the context of the aspect’s metadata is freely available in the
XML document.

The second section is qualified with a Subscriptions tag. This section
contains all the subscriptions inside the domain class. As we remember, a
subscription connects an event raised by one aspect to an event handler on
another aspect. In our example only one subscription exists inside the subs
cription section: The Due event of the Deadline aspect is connected to the
Notify event handler of the Notify aspect.

The final section is qualified with a Delegates tag. This section contains
small chunks of code that get called by the aspects on specific occasions.
In the example a single delegate is created that returns a text string whene
ver the Notify aspect needs to know its Message.

As we can see there is no real difference between the semantics des
cribed in the original C# code and in the XML document. The real differe
nce is that XML is much easier to read and manipulate through XPath and
XSL stylesheets. It is also easy to validate that the XML document is synt
actically correct by using an XML schema.


<!-- Page 256 -->


### 6.9 A Model-Based Framework 247

Let us start by (re)creating the C# code for the domain class using the
following XSL stylesheet:
<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="text" />
<!-
This is the main body of the class
-->
<xsl:template match ="/Class">
public class <xsl:value-of select ="@name"/> {
<xsl:apply-templates select="Aspects/*" />
public <xsl:value-of select ="@name"/>() {
<xsl:apply-templates select="Subscriptions/Subscription"/>
<xsl:apply-templates select ="Delegates/Delegate"/>
}
}
</xsl:template>
<!--
each aspect provides its own code snippet.
in practice the code only consists of a field declaration.
the actual code is placed in a separate aspect class
-->
<xsl:template match="Identifier">
public Identifier <xsl:value-of select="@name"/> =
new Identifier("<xsl:value-of select="../../@name" />" +
"<xsl:value-of select="@name"/>Sequence",
<xsl:value-of select="@seed"/>,
<xsl:value-of select ="@step"/>
ji
</xsl:template>
<xsl:template match="Description">
public Description <xsl:value-of select="@name"/> =
new Description()?
</xsl:template>
<xsl:template match="DueDate">
public DueDate <xsl:value-of select="@name"/> =
new DueDate(<xsl:value-of select="@days"/>)i
</xsl:template>
<xsl:template match="Notification">
public Notification <xsl:value-of select="@name"/> =
new Notification()ji
</xsl:template>


<!-- Page 257 -->


248 6 An Aspect-Based Example Application
<!--
the Map section provides the weaving between event sources
and event targets
-->
<xsl:template match ="Subscription">
<xsl:value-of select ="@source"/>.
<xsl:value-of select ="@sourceevent"/> +=
<xsl:value-of select ="@target"/>.
<xsl:value-of select ="@targetevent"/>;
</xsl:template>
<xsl:template match="Delegate">
<xsl:value-of select="@target"/>.
<xsl:value-of select="@property"/> +=
delegate {
<xsl:value-of select ="text()"/>7
yi
</xsl:template>
</xsl:stylesheet>

By applying the stylesheet to the XML document, the following output
is produced (the output has been reformatted for easier reading. It is
somewhat difficult to get style sheets to create exactly the indentations and
line breaks you want. This is usually no problem however because most
compilers disregard indentations and line breaks anyway):
public class Task {

public Identifier ID =

new Identifier("TaskIDSequence", 10000, 1000);
public Description Text = new Description()i
public DueDate Deadline = new DueDate(20);
public Notification Notify = new Notification()?
public Task() {

Deadline.Due += Notify.Notify;

Notify.Message += delegate {

return Text.Value + " is due";

yi

}
}

This is exactly the same code that we created manually in the beginning
of the chapter. But now that we have an XML document describing our
domain class we may as well create a static user interface instead of relyi
ng on reflection. We can achieve this by using the following XSL
stylesheet:


<!-- Page 258 -->


### 6.9 A Model-Based Framework 249

<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http: //www.w3.org/1999/XSL/Transform">
<xsl:output method="text"/>
<xsl:template match ="Class" xml:space ="preserve">
public partial class Forml : Form {
public Forml(<xsl:value-of select ="@name" />
<xsl:value-of select="@name"/>) {
InitializeComponent () i
Padding = new Padding(5)ji
<xsl:for-each select ="Aspects/*">
<xsl:value-of select ="name()" />Control
<xsl:value-of select ="@name" /> =
new <xsl:value-of select ="name()" />Control()i
<xsl:value-of select ="@name" />.Dock = DockStyle.Top?
<xsl:value-of select ="@name" />.Initialize(
<xsl:value-of select ="../../@name" />.
<xsl:value-of select ="@name" />,
"<xsl:value-of select ="@name" />"
di
Controls.Add(<xsl:value-of select ="@name" />)i
<xsl:value-of select ="@name" />.BringToFront()i
</xsl:for-each>
}
}
</xsl:template>
</xsl:stylesheet>
Applying this stylesheet to the XML document produces the following
output:
public partial class Forml : Form {
public Forml(Task Task) {
InitializeComponent () i
Padding = new Padding(5)i
IdentifierControl ID = new IdentifierControl()i
ID.Dock = DockStyle.Topi
ID.Initialize(Task.ID,"ID");
Controls.Add(ID) i
ID.BringToFront()ji
DescriptionControl Text = new DescriptionControl()i
Text .Dock = DockStyle.Topi
Text .Initialize(Task.Text, "Text")ji
Controls.Add(Text) i
Text .BringToFront()ji
DueDateControl Deadline = new DueDateControl()i
Deadline.Dock = DockStyle.Topi
Deadline.Initialize(Task.Deadline, "Deadline");
Controls.Add(Deadline) i
Deadline.BringToFront()i


<!-- Page 259 -->


250 6 An Aspect-Based Example Application
NotificationControl Notify = new NotificationControl()i
Notify.Dock = DockStyle.Topi
Notify.Initialize(Task.Notify, "Notify");
Controls.Add(Notify)ji
Notify.BringToFront()i

}

}

This code is different from the user interface code that we created previo
usly. The difference is that we do not use reflection but instead create
each aspect control explicitly. Because this code is automatically generated
using the XSL stylesheet we still maintain the ability of the user interface
to adapt to any domain class without having to rewrite the code manually.

Let me demonstrate this by applying a small change to our task mana
gement system: I want the system to send me a reminder five days before
the task is due. I can do this by just adding another DueDate aspect to the
Task class and providing a few new subscriptions:
<Class name="Task">

<Aspects>
<Identifier name="ID" seed="10000" step="1000"/>
<Description name="Text" />
<DueDate name="Reminder" days="15"/>
<DueDate name="Deadline" days="20"/>
<Notification name="Notify"/>
</Aspects>
<Subscriptions>
<Subscription source="Deadline" sourceevent="Due"
target="Notify" targetevent="Notify" />
<Subscription source="Deadline" sourceevent="Completed"
target="Reminder" targetevent="Cancel" />
<Subscription source="Reminder" sourceevent="Due"
target="Notify" targetevent="Notify" />
</Subscriptions>
<Delegates>
<Delegate target="Notify" property="Message">
if (Deadline.State == DueDate.States.Due) {
return Text.Value + " is due";
} else {
return Text.Value + " will be due on " + Deadline.Datei
}
</Delegate>
</Delegates>
</Class>

The first new subscription instructs the Reminder to be cancelled if the
user marks the Deadline as completed. This is important because otherwise
the user would receive a reminder even if she had already completed the
task. The other new subscription instructs the Notify aspect to send the
user an Email when the Reminder aspect raises its Due event.


<!-- Page 260 -->


### 6.10 Storage 251


A small change has also been made to the notification message delegate.
The purpose of this change is to make sure that the user knows whether the
task is already due or if the notification is just a reminder.

By reapplying the two XSL stylesheets specified above to recreate the
domain class and the user interface respectively we end up with the user
interface shown in Fig. 163.

2 Tsk Cee)
ID
10000
Text
Pay the phone bill john@doe.com (&)
Pay the phone bill will be due on 19-01-2006
Reminder f ]
Aorl 30.2006 iv
Deadline
January 20,2006 ¥
Notify
eMail |ohn@doe.com
Fig. 163. User interface of the task aspect

### 6.10 Storage


The final issue that we need to address in order to have a completely
working task management system is how to store and retrieve the tasks
from a database.

We could of course use a “traditional” O/R mapper (such as NHibernate
or Gentle.Net) for this task, but we could also take advantage of the fact
that we already have a domain model of our system to create the storage
code ourselves.

The first question that we need to answer is: how should the tasks be
stored in the database?

The easiest solution to this question is to create a Task table with the
columns shown in Fig. 164:


<!-- Page 261 -->


252 6 An Aspect-Based Example Application
IN
Stores the state of the ID
aspect. Because the ID is an
Identification aspect this
becomes the table’s primary
ee key
Task ef
Colurm Name... Data Type Stores the state \\
gD Oo int | Of the Deadline
DeadlineDate o--gatatine | eee | aspect
DeadlineState Ont N
Stores the state
Notify Omar eRaF{ 50) nd Of the Notify
Text Om... ntext aspect
~~) Stores the state
of the Text aspect
Fig. 164. A task table
What we need to do now is to create an XSL stylesheet that will provide
the necessary code to store and retrieve Tasks from this table. To keep the
example simple only the “create” and “retrieve” methods of the CRUD int
erface will be provided:
<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http: //www.w3.org/1999/XSL/Transform">
<xsl:output method="text"/>
<xsl:template match ="Class" xml:space ="preserve">
public class <xsl:value-of select="@name"/>Facade {
public static List&lti<xsl:value-of select="@name"/>&gti
GetAll(SqlConnection connection) {
List&lti<xsl:value-of select="@name"/>&gti result =
New List&lti<xsl:value-of select="@name"/>&gti()i
SqlCommand command = connection.CreateCommand( ) i
command.CommandText = @"select
<xsl:for-each select="Aspects/*">
<xsl:choose>
<xsl:when test ="name() = 'DueDate'">
<xsl:value-of select="@name"/>Date,
<xsl:value-of select ="@name"/>State
</xsl:when>
<xsl:otherwise>
<xsl:value-of select ="@name"/>
</xsl:otherwise
</xsl1:choose>
<xsl:if test="position() != last()">,</xsl:if>
</xsl:for-each>
from <xsl:value-of select="@name"/>"j
using (SqlDataReader reader = command.ExecuteReader()) {


<!-- Page 262 -->


### 6.10 Storage 253

while (reader.Read()) {
<xsl:value-of select="@name"/> item =
new <xsl:value-of select="@name"/>()i
<xsl:for-each select="Aspects/*">
<xsl:choose >
<xsl:when test="name() = 'DueDate'">
item.<xsl:value-of select ="@name"/>.Date =
(DateTime) reader["
<xsl:value-of select ="@name"/>Date"]i
item.<xsl:value-of select ="@name"/>.State =
(DueDate.States) reader["
<xsl:value-of select ="@name"/>State"]i
</xsl:when>
<xsl:when test="name() = 'Identifier'">
item.<xsl:value-of select ="@name"/>.Value =
(int) reader["<xsl:value-of select ="@name"/>"]i
</xsl:when>
<xsl:when test ="name() = 'Notification'">
item.<xsl:value-of select ="@name"/>.
EMailAddress = (string) reader["
<xsl:value-of select ="@name"/>"]ji
</xsl:when>
<xsl:otherwise>
item.<xsl:value-of select ="@name"/>.Value =
(string) reader["
<xsl:value-of select ="@name"/>"]ji
</xsl:otherwise>
</xsl:choose>
</xsl:for-each>
result .Add(item);
}
}
return result;
}
public static void Insert (
<xsl:value-of select ="@name"/> item,
SqlConnection connection) {
SqlCommand command = connection.CreateCommand( ) i
command.CommandText =
@"insert into <xsl:value-of select ="@name"/> (
<xsl:for-each select="Aspects/*">
<xsl:choose >
<xsl:when test="name() = 'DueDate'">
<xsl:value-of select ="@name"/>Date,
<xsl:value-of select ="@name"/>State
</xsl:when>
<xsl:otherwise >
<xsl:value-of select ="@name"/>
</xsl:otherwise>
</xsl:choose>
<xsl:if test ="position() != last()">,</xsl:if>
</xsl:for-each>
) values (
<xsl:for-each select="Aspects/*">
<xsl:choose >
<xsl:when test="name() = 'DueDate'">
@<xsl:value-of select ="@name"/>Date,


<!-- Page 263 -->


254 6 An Aspect-Based Example Application
@<xsl:value-of select ="@name"/>State
</xsl:when>
<xsl:otherwise >
@<xsl:value-of select ="@name"/>
</xsl:otherwise>
</xsl1:choose>
<xsl:if test ="position() != last()">,</xsl:if>
</xsl:for-each>
"a
<xsl:for-each select ="Aspects/*">
<xsl:choose >
<xsl:when test="name() = 'DueDate'">
command. Parameters .AddWithValue(
"@<xsl:value-of select ="@name"/>date",
item.<xsl:value-of select ="@name"/>.Date
i
command. Parameters .AddWithValue(
"@<xsl:value-of select ="@name"/>state",
item.<xsl:value-of select ="@name"/>.State
i
</xsl:when>
<xsl:when test="name() = 'Notification'">
command. Parameters .AddWithValue(
"@<xsl:value-of select ="@name"/>",
item.<xsl:value-of select ="@name"/>.EMailAddress
i
</xsl:when>
<xsl:otherwise>
command. Parameters .AddWithValue(
"@<xsl:value-of select ="@name"/>",
item.<xsl:value-of select ="@name"/>.Value
i
</xsl:otherwise>
</xsl1:choose>
</xsl:for-each>
command. ExecuteNonQuery()i
}
}
</xsl:template>
</xsl:stylesheet>
Admittedly this stylesheet is a bit complicated due to its multiple fore
ach tag, but the output is still very simple (and hopefully readable):
public class TaskFacade {
public static List<Task> GetAll(SqlConnection connection) {
List<Task> result = new List<Task>()i
SqlCommand command = connection.CreateCommand( ) i
command.CommandText = @"select ID,
Text,
DeadlineDate,
DeadlineState,
Notify
from Task";
using (SqlDataReader reader = command.ExecuteReader()) {
while (reader.Read()) {
Task item = new Task();


<!-- Page 264 -->


### 6.11 Storing Aspect Data in Separate Tables 255

item.ID.Value = (int)reader["ID"]i
item.Text.Value = (string)reader["Text"];
item.Deadline.Date =(DateTime)reader["DeadlineDate"]i
item.Deadline.State =
(DueDate.States)reader["DeadlineState"]i
item.Notify.EMailAddress = (string)reader["Notify"];
result .Add(item) i
}
}
return result;
}
public static void Insert(Task item, SqlConnection connection) {
SqlCommand command = connection.CreateCommand( ) i
command.CommandText = @"insert into Task (ID,
Text,
DeadlineDate,
DeadlineState,
Notify
) values (@ID,
@Text,
@DeadlineDate,
@DeadlineState,
@Notify
) "a
command. Parameters.AddWithValue("@ID", item.ID.Value) i
command. Parameters .AddWithValue("@Text", item.Text.Value) i
command. Parameters .AddWithValue("@Deadlinedate",
item.Deadline.Date) i
command. Parameters .AddWithValue("@Deadlinestate",
item.Deadline.State);
command. Parameters.AddWithValue("@Notify",
item.Notify.EMailAddress) i
command .ExecuteNonQuery ( ) 7
}
}

The nice thing about this code is that by reapplying the XSL stylesheet
to the XML document representing the domain model we can always crea
te storage code that reflects the domain model’s actual definition. There is
a catch however: While the storage code is automatically updated, this is
not the case with the table definition itself. It is of course possible to autom
atically create the change scripts necessary to update the table definition
as well, but often the database is considered such a valuable asset that
changes to its definition are required to be done manually.


### 6.11 Storing Aspect Data in Separate Tables


Consider the following situation: In a system we have a large number of
DueDate aspects spread out over a number of different domain classes.
Employees have DueDates for employee reviews, salary adjustments, bo-


<!-- Page 265 -->


256 6 An Aspect-Based Example Application

nus payments, etc. Sales orders have DueDates for payment date, shipment
date, etc. In order to find out which of these DueDates are due we need to
select data from a huge amount of tables in the database.

SELECT id FROM employee WHERE employeereviewdate < SYSDATE

AND employeereviewstate = ‘Active’

SELECT id FROM employee WHERE salaryadjustmentdate < SYSDATE

AND salaryadjustmentstate = ‘Active’

SELECT id FROM salesorders WHERE paymentdate < SYSDATE

AND paymentstate = ‘Active’

Wouldn’t it be nice if all the DueDates were collected in a single table?
If this was the case, we could easily retrieve due DueDates using a SQL
statement similar to the following:

SELECT class, id FROM duedates WHERE date < SYSDATE

AND state = ‘Active’

By applying this idea in the extreme, we could come up with a database
model where each aspect had its own table. Such a database model would
look like the model in Fig. 165.

DomainObject *
@ Class
gD
Notification * po
@ Class 3 &

@ ID Description *
Q Name od| 8 Class
EMailAddress 8 g ID

DueDate * v
@ Class
g ID
Q@ Name
Date
State
Fig. 165. Generic database model

There are some benefits to this way of storing data and some drawbacks.

First the benefits:


<!-- Page 266 -->


### 6.11 Storing Aspect Data in Separate Tables 257


e Interestingly it often makes sense to look at an aspect across its domain
classes: DueDates can be plotted in a calendar so that events coming up
can be spotted beforehand, Locations can be plotted on a map as “points
of interest”, and Notifications can be interesting to the recipient as part
of the question “what am I currently subscribing to?”. Such requests that
crosscut several domain classes will usually perform better if all the asp
ects’ data are stored in a single table.

e The database model does not change even when changes are made to the
domain model. This makes it easier to deploy changes to the domain
model.

The drawbacks are:

e Selecting a single object from the database requires several select statem
ents. This has a certain impact on performance.

e Creating complex “where’”-clauses can become almost impossible. It is
also very difficult to write SQL statements that perform well if the
“where’’-clause spans several aspects (e.g., finding all tasks with a due
date in November and with the description containing the text “phone”)
because the database’s execution planner will often resolve this type of
query by performing a Cartesian join.

e Often system integration is performed on a database level. Without the
domain model to “decrypt” the database it will be very difficult for other
applications to make sense of the data in the database.

All in all the best solution will probably be to stick to the conventional
way of storing data and perhaps supplement this by creating redundant

“aspect” tables where it is deemed necessary.


<!-- Page 267 -->


Part lll Modeling Handbook


# Part I of this book, Structural Patterns described the basic concepts of the

REA modeling framework, and how it can be used to create an REA applic
ation model of a business system. Part II, Behavioral Patterns, described
how the application model can be extended to support specific functionali
ty that originates in user requirements.

Our experience shows that usually the most difficult modeling task is to
design an REA application model. Once the application model is created, it
is usually straightforward to extend it with existing behavioral patterns.
REA leads application designers to the solution that conforms to the laws
of the business domain; it is not always straightforward and easy to create
application models that follow the domain rules. To make a sound REA
model, the application designers must often think deeply before they disc
over the essence of the customer’s business.

In this part, Modeling Handbook, we will illustrate examples of REA
application models for elementary exchanges, elementary conversions,
value chains with exchange and conversion processes, and REA models
with contracts.

The first and second sections illustrate REA models of elementary exc
hange and conversion processes at the operational level. The third section
shows examples of processes at the operational level where the model cont
ains both conversion and exchange processes. The fourth section, Proce
sses with Contracts, illustrates examples of REA models at policy level,
which include types, groups, commitments, contracts, and schedules, in
addition to economic events, resources, and agents.


<!-- Page 268 -->


7 Elementary Exchange Processes

This section illustrates REA models of exchange processes at the operat
ional level. These models contain economic events, economic resources,
and economic agents in exchange processes. We describe the REA models
for the following exchange processes: Cash Sale, Product Return, Disc
ounts, Loan and Rent, and Financing.


<!-- Page 269 -->


262 7 Elementary Exchange Processes

### 7.1 Cash Sale

a , ’ >ae
‘ete * ANDIES ’ oe
a ak | Joti wars PARILACY < oh ae t
‘ zd ban “Ss
The sales process is one of creating revenue; therefore, every company has
a process similar to sales. The only exception might be non-profit organiz
ations, but they also have a process of providing services or goods. For
organizations receiving donations, the recipient economic agent of these
services or goods is different from the economic agent providing cash, but
the basic model remains the same.
Cash Sale is the simplest version of the sales process, and is applicable
to sales in shops or sales to walk-in customers.
Problem
How do we create an REA application model for the cash sale process?
Solution
A sales process is an exchange of products for cash. The value chain
model for a cash sale process is illustrated in Fig. 166.
Prod «exchange process» Cash
rodu Sales
Fig. 166. Value chain model for the cash sale process
The REA model in Fig. 167 illustrates a scenario known from retail
shops, where a customer buys a product and pays cash. This scenario does
not require modeling contracts such as a sales order.


<!-- Page 270 -->


### 7.1 Cash Sale 263


This model contains two economic events: Sale and Cash Receipt. The
economic event Sale is related through an exchange duality with economic
event Cash Receipt. Each instance of Sale is related to exactly one Produ
ct. Sale represents change of ownership of Product from enterprise to
Customer. Likewise, each instance of Cash Receipt is related to exactly
one Cash instance, which represents, for example, an amount of money in
a specific currency.

In general, the relationship between Sale and Cash Receipt is many-tom
any; several instances of Sale (a sale of several products) can be related
to several instances of Cash Receipt (for example, customer pays cash in
different currencies).

As Sale does not have to happen at exactly the same time as Cash Rec
eipt (for example, payment often occurs after entries of all goods go
through the cash register), the Claim represents the value of the imbala
nced exchange duality. This value can be displayed on a cash register, or
made in some other way available to the participating economic agents.

1 «claim» 1
Amount to
Pay
sinkisions «terminator»
0..* 0..*
«outflow» «exchange» «inflow»
1 0..* Sale o.* 0..*| Cash Receipt | 0..* 1
0." 0..* 0." Toe
«receive» «provide» «receive»
1 1 «provide» 1 1
Customer Enterprise
Fig. 167. Sales Process
If a customer pays cash, he usually gives the enterprise an amount higher
than is claimed. The enterprise then returns the excess payment to the cust
omer to settle the claim. If users of business applications are interested in
keeping track of the money returned to customers, the model must be
modified by adding a decrement event, Cash Return, as shown in Fig. 168.
The economic agents are the same as in Fig. 167.


<!-- Page 271 -->


264 7 Elementary Exchange Processes

Cash return is possible also in the model illustrated in Fig. 167, but in
this model the business application does not keep track of the money ret
urned, while in the model in Fig. 168 it does.

1 f
«claim» 1
Amount to
i | | Pay
«initiator» «initiator» «terminator»
0..* 0..*
«outflow» «increment» «inflow»
«decrement» Cash
1 0..* Sales Receipt 0..* 1
«resource» 0 xs
.* «resource»
Product «exchange duality» Cash
«decrement»
o..*| Cash Return 1
«outflow»

Fig. 168. Cash sales with tracking cash returns


<!-- Page 272 -->


### 7.2 Product Return 265


### 7.2 Product Return

Of 0
100% :100 %e
pct - GUARANTEE
Many companies allow customers under certain conditions to return Purc
hased products. Users of business applications would like to track and
create reports on economic events related to returns of products.
Problem
How do we model returns of purchased items?
Solution
The return of products can occur only if the products have already been
sold, and users of business applications might not consider it as a valuea
dding process. Therefore, we model the return of products as an economic
event that is part of the sales process. Its value chain is shown in Fig. 169.
Product = «exchange process» => Cash
uN Sales =

Fig. 169. Value chain for the sales process

The REA model for a sales process with the return of products is illust
rated in Fig. 170. The exchange duality is a 4-ary relationship between
economic events Sale, Cash Receipt, Product Return, and Cash Return.
All four events contribute to the claim Amount to Pay; Sale and Cash Ret
urn increase the value of the claim and Product Return and Cash Receipt
decrease the value of the claim. The model specifies that the value of the


<!-- Page 273 -->


266 7 Elementary Exchange Processes
Sale plus Cash Return should be equal to the value of the Product Return
plus Cash Receipt.

When a customer returns a product (i.e., the enterprise accepts and regi
sters the product return), a positive claim is raised, and business practice
determines how the enterprise is going to settle the claim.

e The enterprise can materialize the claim, i.e., issue a credit note to the
customer.

e The enterprise can sell to the customer another product, usually of the
same or equivalent type, that settles the claim.

e The enterprise can return cash to the customer.

Some companies do not return the full purchase price of the product to
the customer in the case of a return. In the model in Fig. 170, this means
that the returned product has less value than the sold product. Conseq
uently, the amount of cash returned is less than the amount of cash rec
eived.

1 1
«claim»
. 7] Amount to 1 «initiator»
«terminator» Pay
«resource» «initiator» «terminator» «resource»
Product 0.* 0..* Cash
1 1 «decrement» «increment» 1 1
«outflow» 0..* Sale 0." 0..* | Cash Receipt | | 9.* «inflow»
«inflow» 0..* *
- 0." & 0..* 0.. «outflow»
«increment» y «exchange «decrement» | 0..*
0.* rouct duality» Cash Return
Fig. 170. Sales process with return of products

If the product sold and returned is an individually identifiable item (its
quantity is measured in pieces as opposed to kilograms or joules), there is
a one-to-one relationship between the return event and the sale event. The
sale with product return can be considered a kind of sale, and the model
can be simplified, as shown in Fig. 171.


<!-- Page 274 -->


### 7.2 Product Return 267

1 «claim» 1
Amount to
Pay 1 «initiator»
wae «terminator»
«initiator»
«resource» 0..*
Product
a." 0..* | «increment» | | «inflow» «resource»
1 Cash Receipt | |0..* 1 Cash
«outflow» «decrement»
0..* Sale OF <> 0..* 1
«exchange ~~ «decrement» «outflow»
duality» 0. Cash Return | 0..*

Fig. 171. Return of individually identifiable items

The Sale event is now a time interval; at the beginning of the sale event,
the product’s ownership transfer from enterprise to customer, and at the
end of the interval, it transfers back from customer to enterprise. The Sale
event is still a decrement event, because it decreases the value of the produ
ct for the enterprise for several reasons. For example, during the time int
erval between sale and return, the product cannot be sold to another cust
omer.


<!-- Page 275 -->


268 7 Elementary Exchange Processes

### 7.3 Loan and Rent (Individually Identifiable Resources)

a r 1 |

FOR RENT

ee See =
To rent an economic resource means to grant the possession of the res
ource in return for the payment of rent from the tenant, and for the tenant
to take and hold the resource (property, machinery, etc.) in return for the
payment of rent to the landlord or owner. The grant is always temporary;
the tenant must eventually return the rented resource to the owner; howe
ver, the length of the rental period can be unspecified.
Problem
What is loan and rent in the REA terms?
Solution
The loan or rent process is an exchange of rights to use an economic res
ource for cash. The value chain model for rental is illustrated in Fig. 172.
Note that the arrow means change of value of resources, not physical flow
of resources. Renting a property decreases its value for the owner; for exa
mple, it cannot be rented during the rental period, or the owner does not
keep full rights to the rented resource. The owner receives cash in return.

Rental

Fig. 172. Value chain model for loan and rental
The REA model for rental and loan is illustrated in Fig. 173. The enterp
rise in this model is renting an economic resource, Property, in exchange
for Cash.


<!-- Page 276 -->


### 7.3 Loan and Rent (Individually Identifiable Resources) 269

«economic agent» «economic agent»
Owner Enterprise
«receive» «receive» ,
«provide» «provide»
aresourcen | “inflow») Gincrement» |“exchange»| «decrement» |“Utflowy resource»
Property Rental Rent Payment Cash
Fig. 173. The REA application model for the rental process
A timing diagram with an example of one Rental and two Rent Paym
ents is illustrated in Fig. 174.
Rental: Property is under the control of the enterprise
Property is under the Property is under the
control of the owner control of the owner
Rent Payment (first payment): Cash is under the control of the owner
Cash is under the control
of the enterprise
Cash is under the control of the owner
Rent Payment (second payment):
Cash is under the control of the enterprise
time
tS
Fig. 174. The timing diagram for an example of the rental process
The increment event Rental is an economic event with duration equival
ent to the rental period. At the beginning of this economic event, the usage
rights of the economic resource Property are transferred from the Owner to
the Enterprise, and at the end of this event the usage rights are transferred
back from the Enterprise to the Owner. The Rental event is paired through
an exchange duality with the instantaneous Rent Payment event, which
causes outflow of economic resource Cash. The exchange is a many-to-


<!-- Page 277 -->


270 7 Elementary Exchange Processes

many relationship. There can be several Rent Payment events for a single
Rental event. Also, several Rentals can be paid for by one or more Rent
Payments.

The rental process is similar to the financing process discussed in the
next chapter. However, the models for rental and financing are different;
compare Figs. 173 and 176. The reason for this difference is that Property
in Fig. 173 is an individually identifiable resource (received and returned
as a whole unit), while Cash in Fig. 176 is not.


<!-- Page 278 -->


### 7.4 Financial Loan (Nonindividually Identifiable Resources) 271


### 7.4 Financial Loan (Nonindividually Identifiable

Resources)
«~~ oe ~o\ |
Sor oehy vo
There are many ways an enterprise can receive the financial resources it
needs. We will illustrate a simple form of financing in which the enterprise
borrows money from the bank for a specific period. The bank receives int
erest as a compensation for the loan. For the enterprise, the money it borr
ows has more value than the interest; for the bank, the interest has more
value than the money it lends.
Problem
What is the financing process in REA terms?
Solution
The financing process is an exchange of cash for cash. The enterprise rec
eives cash for a limited period of time. Eventually, it returns the cash and
also pays interest for the loan. The value chain model is illustrated in
Fig. 175.
«exchange process»

Fig. 175. Value chain model for financing

The REA model for a financial loan is illustrated in Fig. 176. The enterp
rise receives an economic resource, Cash, in the Loan Receipt event. This
event is paired in duality with two economic events, Loan Return, in which
the Cash is returned back, and Interest Payment, in which the enterprise


<!-- Page 279 -->


272 7 Elementary Exchange Processes

pays additional cash to the bank as a compensation for the Loan. The
whole process can at runtime consist of several Loan Receipt events, seve
ral Loan Return events, and several Interest Payment events. The dates
for these evens can be specified by commitments, which are part of the
contract. The REA model at the operational level does not contain any res
trictions on the dates these events occur, or in what order, but commitm
ents that are part of the financing contract usually specify the Loan Rec
eipt, Loan Return, and Interest Payment dates.

Bank Enterprise
«receive» «receive» «receive» «provide»
«provide» a «provide»
«resource» |«inflow» «increment» | «exchange» “aecrement»
Cash Loan Receipt <s Payment |
«outflow»
Loan Return
«outflow»
Fig. 176. The REA application model for financing

The financial loan is similar to the rental discussed in the previous chapt
er. However, the models for rental and financing are different; compare
Figs. 173 and 176. Can we model rental using the model in Fig. 176?

The reason for the different models is that in the model in Fig. 173 the
resources are individually identifiable, while in the model in Fig. 176, they
are not. Generally, the REA models represent economic exchanges, i.e.,
some resources are exchanged for others. If we model rental according to
Fig. 176, i.e., with a model containing three economic events, instantaneo
us increment Start Property Rental, and instantaneous decrements End
Property Rental and Pay Rent, the pair Start Property Rental and End
Property Rental is not an exchange; the renter is returning the same prope
rty back he rented. Therefore, we prefer to model the rental of a property
as one economic event with duration.

On the other hand, in the case of a loan, the economic resource Cash is
not individually identifiable; there is no way to determine whether the cash


<!-- Page 280 -->


### 7.4 Financial Loan (Nonindividually Identifiable Resources) 273

received is the same as the cash returned. In fact, banks usually allow rente
rs to return a different Cash Type than the one they gave to the renter; for
example, a loan can be given as a check and returned by bank transfer.
Therefore, as received cash and returned cash might be different, Loan Rec
eipt and Loan Return are different economic events in the case of Cash
and other resources that are not individually identifiable. For example,
gasoline loaned can be different one is returned (the individual molecules
in loaned and returned gasoline will be different), therefore, we would
model the loan of gasoline similarly to the model illustrated in Fig. 176.
The loan of gasoline would be one economic event, and its return another
economic event.

If the enterprise borrows an item (individually identifiable resource) and
buys another item of the same type, it is always possible to distinguish
which item is borrowed and which is owed. On the other hand, if the ent
erprise stores borrowed and owed cash in one bank account, it is impossib
le to distinguish the cash borrowed from the cash owed (the amounts of
owed and borrowed cash can be determined only by examining the econ
omic events that changed the amount of cash in this bank account). This
difference also indirectly explains why the two models are different.

Another, practical reason why the two models are different is that indiv
idually identifiable resources must be returned complete, in one piece,
when the rental period ends. However, resources that are not individually
identifiable can be returned in different quantities. For example, a renter
can pay for the loan in installments. The model in Fig. 176 allows modeli
ng the installments, while the model in Fig. 173 does not.


<!-- Page 281 -->


8 Elementary Conversion Processes

This section illustrates REA models of conversion processes at the opera-

tional level. CREATING A NEW PRODUCT is a fundamental conversion

process in which a new instance of an economic resource is created from
other resources.

If the conversion process consists of phases, and users of business applic
ations would like to plan, monitor, and control the work in progress and
intermediate resources, the process can be split into finer-grain processes
in two different ways.

e The CHAIN OF CONVERSION PROCESSES is essentially a sequence
of processes in which an intermediate product is created and then cons
umed by the next process in the chain. This process is convenient if the
subsequent processes entirely change the identity of the resource.

e The modeling approach MODIFYING A RESOURCE is suitable in
situations in which the process does not change the identity of the produ
ct; for example, only makes some modification of it.

The concept of services in the process CREATING AND CONSUMING

SERVICES can be used to introduce a level of indirection into a chain of

processes, and to represent the results of some processes as a service. This

is useful, for example, in outsourcing some conversion processes, and will
be described in the section on combined models.


<!-- Page 282 -->


276 8 Elementary Conversion Processes

### 8.1 Creating a New Product

SITIN LPPITIIIT _ LIEIPIZ® Ir

ROW oy BO een
Almost every company has a process in which it creates a new service or
product. The new product or service is an economic resource, and for its
creation the enterprise uses or consumes other economic resources.

When creating an REA model for a conversion process, an application
designer must answer the following question.

Problem

How do we make an REA application model for a conversion process that
creates a new product?

Solution

The output of the conversion process is an economic resource that users of
a business application want to monitor and control. One of the outputs is a
product, but many conversion processes produce other resources, such as
waste. Whether or not to model these resources is a decision of the users of
the business application, and is the result of their needs for information
about these resources.

We will illustrate the process in a scenario in which the new product is
produced, and then inspected for quality. The assembly process encomp
asses all technological operations of assembling the product, using tools.
The assembled product is then inspected for defects.

In this scenario, users of a business application are not interested in
planning, monitoring and controlling the work in progress and intermedia
te resources. Therefore, the assembly and quality inspection are combined
into a single conversion process. The value chain model is shown in
Fig. 177.


<!-- Page 283 -->


### 8.1 Creating a New Product 277

Part
«conversion process»
Tool \repecton Product
Labor —
Fig. 177. Value chain model for creating a new product

The REA application model for this process is illustrated in Fig. 178.
Resources consumed in this process are Part and Labor (they will not exist
after the end of the process), and Tool is a resource used (that can be used
again). The result of the process is the Product resource. In this example,
we have decided to model Part and Product as different entities. However,
in many business applications it is not always like this; there is often an
entity, usually called Item, that represents all economic resources of a
physical nature.

The Material Issue economic event consumes the Part resource. During
this economic event, the Part is transformed into the Product. The prov
ider economic agent Employee is responsible for Part before it has been
issued, and the recipient agent Supervisor is responsible for Part until it
becomes part of the Product.

The Tools Usage economic event uses the economic resource Tool in
order to assemble and inspect the Product. In this example, we assume that
the tools are picked up by the workers and returned after the assembly
process is finished; we model the Enterprise as a provider agent, responsib
le for the Tool before and after the Tool Usage event, and model the
Worker as a recipient economic agent responsible for the tool during the
event.

The Labor Consumption economic event consumes the economic res
ource Labor and transforms it into the Product. The provider economic
agent is Supervisor, who is authorized to decide upon labor consumption
during the process. The recipient economic agent is Worker, who is res
ponsible for his own labor during the Labor Consumption economic
event.


<!-- Page 284 -->


278 8 Elementary Conversion Processes
cent «economic «economic «economic
Warohe agent» agent» agent»
ole Supervisor Worker Supervisor
«receive» «provide» «receive»
; «provide»
«provide»
« » «consume» « » «increment»
” ” ” Inspection
. x ; 0..*
«resource» «decrement» 0. conversion
Tool «use» Tools Usage
1 0." «produce»
« » «consume» «decrement» 0..*
” Consumption
«resource»
«receive» «receive» «provide»
«economic «economic
agent» agent»
Worker Supervisor
Fig. 178. The REA model for creating a new product in a single process
An instance model is illustrated in Fig. 179. The resulting economic res
ource is Product with a serial number P3, assembled of two Parts with
serial numbers /22 and /23. These parts have been issued at times
7:20 a.m. and 7:25 a.m., respectively, by Warehouse Clerk Ethel, and
given to Worker Moe. Worker Moe came to work at 7:00 a.m., but started
work at 7:20 a.m., when he got the task and material from his Supervisor
Andy; the enterprise acquired Andy’s Labor from 7:00 a.m. to 15:00 a.m.,
but Consumed only 60 minutes, from 7:30 a.m. to 8:30 a.m., on Producing
the Product P3. The rest of Moe’s Labor was spent on activities beyond
the scope of the model in Fig. 179. At 7:40 a.m. Moe picked the Tool T5
and returned it back at 8:15 a.m. Andy started assembling and inspecting
the product at 7:50 a.m. and finished at 8:30 a.m.


<!-- Page 285 -->


### 8.1 Creating a New Product 279

[Name = Ethel | ID = Andy
«provide» «provide» <«receive»| «provide» «receive»
|
| «provide» «receive»
«decrement» «increment»
ees Time = 7:20 Inspection
Time = 7:50-8:30
Material |
ID = 123 Time = 7:25 «conversion
duality» «produce»
Tool «decrement»
_ Tools Usage
«use»
ID =T5 Time = 7:40-8:15
ID=P3
«decrement»
Nnsumption
Time = 7:00-15:00 Time = 7:30-8:30
, «receive» «provide»
«receive»
«economic «economic
agent» agent»
Worker Supervisor
|D=Moe ID = Andy
Fig. 179. An instance model for creating a product

The creation of a resource is not instantaneous. If users of business app
lications are interested in modeling various stages of the production, but
not interested in the intermediate products, they might use tasks to model
the production process at a finer level of granularity than that of the level
of economic events. We have not illustrated this modeling aspect in this
book; we intend to describe it on our web page.

The model in Fig. 178 does not have an entity for intermediate products,
work in process inventory and such. If users of business applications are
interested in planning, monitoring, and controlling the intermediate produ
cts within the production process, application developers must split the
production process into a chain of several value-adding processes. This is
illustrated in the models in the following pages.


<!-- Page 286 -->


280 8 Elementary Conversion Processes


### 8.2 Chain of Conversion Processes


Fo aa

Users of business applications are often interested in planning, monitoring,
and controlling intermediate products that are produced in various stages
of the overall production process.

Problem

How do we develop an REA application model that allows for planning,
monitoring, and control of intermediate products, under the assumption
that the intermediate products are consumed as they are converted to the
final product?

Solution

Split the overall conversion process into a chain of smaller conversion
processes. An economic resource produced in the first process is consumed
in the second process, and so on. The last process in the chain produces the
final product.

We will illustrate this approach on the same example as in the previous
chapter, but with the additional requirement that users of the business app
lication would like to keep track of the products that have been assembled
but have not been inspected for quality.

The production of a final product consists of two processes: the assemb
ly process creates the assembled product, and the inspection process cons
umes the assembled product and creates the final product. The value
chain model is shown in Fig. 180.


<!-- Page 287 -->


### 8.2 Chain of Conversion Processes 281

Part «conversion process»
Tool _——__=> Assembly
Assembled Product
L «conversion process»
abor Quality Inspection ~ Final Product
Fig. 180. Value chain model for the chain of conversion processes
«conversion process» Assembly
«resource» | <consume» «decrement» «increment» «produce»
Part Material Issue Assembly
«resource» ial «decrement» «conversion “TESOUICE™
Tool Tools Usage duality» Assembled
Product
«conversion process» Quality Inspection
«consume»
‘“resource» «produce»| «resource» | “9fouping>
Labor Final
«decrement» «conversion Product
Material Issue duality»
«consume» S >
«decrement» «increment» «group»
Labor Inspection Quality Group
Consumption
Fig. 181. The REA application model for the chain of conversion processes
The REA application model is illustrated in Fig. 181. For simplicity, we
assume that the assembly process does not consume labor; it is fully autom
ated. The quality inspection process encompasses all necessary quality
inspection activities; for simplicity we assume that this process only cons
umes labor. The result of the quality inspection is the classification of the
product into a quality group, such as first quality, second quality, and
waste. We can consider this classification as a feature of the product that is
changed by the quality inspection process.
The economic agents are the same as in Fig. 178 and are omitted in
Fig. 181.


<!-- Page 288 -->


282 8 Elementary Conversion Processes
The model in Fig. 181 has the following features:

e There is an explicit dependency between processes, which implies the
time order of the processes. The model implies that the product must be
first assembled and then inspected.

e The assembled product and the inspected product are different entities.
They might have different type, description, serial number, and set of
features.

e The assembled product and the inspected product can be related each to
a different process. For example, the model above specifies that the ass
embled product can be inspected while the final product cannot be ins
pected (it is not consumed by the inspection process). Similarly, by rel
ating the final product to the sales economic event, we specify that the
final product can be sold, while the assembled product cannot be sold.

In reality, the inspection process changes only one intangible feature of
the product, its classification, into a quality group. Therefore, it might look
inappropriate that in the model in Fig. 181 the inspection process cons
umes (i.e., destroys) the assembled product and creates a new final produ
ct. This is a rather philosophical statement, and not a strict rule about how
much the features of a thing must change in order for it to be considered a
different thing after the change. Specifically, for modeling the quality ins
pection, we might consider the MODIFYING A RESOURCE model, des
cribed in the following chapter, more intuitive.


<!-- Page 289 -->


### 8.3 Modifying a Product 283


8.3. Modifying a Product
"ty ae 1 ; ue . ¢
Sr {tary *e- IF,
me VEY, Z oe af 3 “

EE Ay
Many conversion processes change only some features of an existing econ
omic resource. Examples are maintenance, transport, and quality inspect
ion processes.

If the conversion changes an economic resource that has been created by
another process or received by an exchange process, there are two different
approaches to describe the conversion process, depending on how much
the resource has changed.

Some experts in the REA modeling framework argue that by changing
any feature of an economic resource the process consumes the old resource
and creates a new one. For example, by the visual inspection of the quality
of a product, the quality inspection process consumes the old product and
produces a new one, because it sets the quality group of the resource. If we
would like to follow this approach, we should use the model described in
the CHAIN OF CONVERSION PROCESSES chapter.

In this book, we will follow a more pragmatic approach by allowing the
same instance of the resource to be both at the input and at the output of
the conversion process.

Problem

How do we make an REA application model for a conversion process that
changes only some features of the existing product?

Solution

Obviously, there must be an economic event related to the resource by the
produce relationship, because the process increases the resource value.
However, it is important to realize that the conversion process that modif
ies the economic resource also uses it.


<!-- Page 290 -->


284 8 Elementary Conversion Processes
We will use the same example as in the previous chapter, but this time
we will model the assembled product and the final product as the same ent
ity. The value chain model is in Fig. 182.
Part «conversion process»
Tool : Assembly
Product
Labor. «conversion process»
Quality Inspection
Fig. 182. Value chain model for modifying a resource
«conversion process» Assembly
«resource» | “CONSUME! — <decrement» «increment»
Part Material Issue Assembly
: Creation of the],
«resourcen | “USS” «decrement» «conversion _| product
Tool Tools Usage duality» «produce»
«group»
Quality
«conversion process» Quality Inspection Group
«resource» «grouping»
«use»
Resource is used [~~
«decrement» 4 Change the I
Material Issue MeO SeieM «produce» quality group
duality» of the product
«resource» |“Consume» «eoen> «increment»
Labor Consumption Inspection
Fig. 183. The REA application model for modifying a resource
The REA application model for modifying a product is shown in
Fig. 183. The model contains an economic resource, Product, created by
the increment economic event Assembly. The Product is used by the decr
ement economic event Issue for Inspection in the Inspection process, and
the increment economic event Inspection modifies features of the product.
The decrement economic event Issue for Inspection expresses the fact that,


<!-- Page 291 -->


### 8.3 Modifying a Product 285

during this event, Product might not be available for purposes other than
Quality Inspection.

Current state of the product, i.e. whether the product is currently assemb
led or inspected can be determined by examining whether the instances of
the relationships to the economic events Assembly and Inspection exist.

The same structure can be used to model transport, maintenance, and
other processes that change some feature of the product, but not its exist
ence.

The model in Fig. 183 has the following features:

e In general, there is no explicit dependency between business processes,
and the model therefore does not imply the time order of the processes
at runtime. We can see that the assembly process must be performed
first, because it creates a product. We know it creates a product because
it does not contain any decrement event that uses the product. However,
if there were several processes modifying a product, such as transport
and storage, in addition to inspection, the processes modifying a product
(quality inspection, storage, and transport) could happen in any order,
and also in parallel.

e The process that modifies the product may occur an arbitrary number of
times. For example, the model allows for a product that has already been
inspected to be inspected again.

e The product is the same entity through the whole chain of processes. For
example, it has the same type and description, the same identity; it has
the same set of features, though production economic events change
their values.

e There is no entity for an intermediate product, but an intermediate
product can be identified. An assembled product is just a state in the
lifecycle of the final product, and can at runtime be identified by exami
ning the produce relationships: the actual products linked to an assemb
ly economic event have been assembled; the actual products linked to
an inspection economic event have been inspected.

The decision whether to use the model for the CHAIN OF
CONVERSION PROCESSES, or for MODIFYING A RESOURCE, depends
on whether the modifying process changes the implementation type of the
economic resource.

For example, if instead of quality inspection we had business processes
for storage or transport, we would probably consider the resource before
the processes the same type and identity as after them. Some features of


<!-- Page 292 -->


286 8 Elementary Conversion Processes
these products will change during transport and storage (at least their cost
attribute will increase), but the set of features will remain the same.

On the other hand, in the process of reconstruction of a building, we
might consider the building before and after the reconstruction as having
different set of features. Therefore, we might consider the reconstruction
as consuming the old building and creating a new one. In this case, the
CHAIN OF CONVERSION PROCESSES model would be more appropria
te than the MODIFYING A RESOURCE model.


<!-- Page 293 -->


### 8.4 Creating and Consuming Services 287


### 8.4 Creating and Consuming Services

ZA y |
Le
pe
F Fa = ~
cA Oe 4

aa - =.
Sometimes, modeling the economic resources that are used or consumed in
order to increment the value of other resources is not possible or desirable.
Instead, we can encapsulate these economic resources in a new economic
resource called service. The service resource is created by using or cons
uming some economic resources, and the service resource is consumed to
increment the value of other resources.

The creation of services can be modeled in a similar way as the creation
of products, the difference is that an enterprise cannot create a service,
store it, and consume it later. Services are economic resources that are crea
ted at the same time as they are consumed. This difference has no influe
nce on the REA modeling principles.

Services as transient resources add a level of indirection between busin
ess processes, but do not change the operational semantics of the chain of
conversion processes. This is useful, for example, if the business process is
outsourced, and its result — service — is purchased or sold to other econ
omic agent.

In the following example, we will illustrate this modeling pattern on the
inspection process. The Quality Inspection process will consume the econ
omic resource Inspection Service, instead of Labor. Inspection Service is
produced by business process Inspection Service Creation; see Fig. 184.

An REA application model is shown in Fig. 185. The business process
Inspection Service contains the increment economic event Inspection Serv
ice Creation, which is paired through conversion duality with the decrem
ent economic event Labor Consumption. The Quality Inspection process
consumes the Inspection Service, and the result, as in previous cases, is the
classification of the Product into a Quality Group.


<!-- Page 294 -->


288 8 Elementary Conversion Processes
Part «conversion process»
Tool Assembly
Product
«conversion process» =
Quality Inspection
Inspection Service
«conversion process»
Inspection Service Labor
Creation
Fig. 184. Value chain for creating and consuming services
«conversion process» Assembly
«resource» |“CONSUME»! — Kdecrement» «incremenb» ‘Quality
Part Material Issue Assembly Group
S Creation of [|
«resource» suse» «decrement» | «conversion the product
Tool Tools Usage duality» ee ;
«produce» «grouping»
«conversion process» Quality Inspection
«resource»
Resource is [\..- «use» Product
use Dy «decrement» . Change the [\
inspection Material issue «conversion __.| quality group
duality» «produce»! of the product
«consume». x
«resource» ‘ enoution . t
Inspection besa ion “ncremen »
Service ervice nspection
Consumption
«conversion process» Inspection Service Creation
«produce»
«conversion
«increment» duality» «decrement |«consume»
«resource»
Inspection Labor Labor
Service Creation Consumption
Fig. 185. The REA model for creating and consuming services


<!-- Page 295 -->


### 8.4 Creating and Consuming Services 289

The REA model with services has the following features:

e Service is a transient resource. It is produced at the same time as it is
consumed; a company cannot store it. If the service is not exchanged
(purchased or sold) with another economic agent, the model can be simp
lified by omitting the service resource and the production and cons
umption of the service. For example, in Fig. 185 we can pair the decr
ement event Labor Consumption through a conversion duality with the
increment event Inspection, and omit the events Inspection Service
Creation, the Inspection Service Consumption and the resource Inspect
ion Service.

e Service is an economic resource; therefore, it can also be related
through an inflow and outflow with other events. For example, a comp
any can produce an inspection service and also purchase some of it
from a subcontractor.

e The model is especially useful if some or all the service is obtained by
an exchange process.


<!-- Page 296 -->


9 Value Chains with Exchange and Conversion
Processes

This section illustrates REA models at the operational level that contain

both exchange and conversion processes.

The SALE AND SHIPMENT chapter illustrates how the sale and shipm
ent processes are related. The chapters PEOPLE MANAGEMENT, and
EDUCATION illustrate that the REA modeling framework can track the
use and consumption of resources that are considered overhead in most
business applications. We will also illustrate how the REA models for
TAXES, WASTE, PURCHASING AND SELLING SERVICES, and
TRANSIENT RESOURCES look.


<!-- Page 297 -->


292 9 Value Chains with Exchange and Conversion Processes


### 9.1 Sale and Shipment


The Sale event means transfer of ownership of a product from the enterp
rise to the customer. The moment of the transfer of ownership must be
agreed upon between the economic agents.

Shipment is a conversion process that changes one of the features of the
product — its location. The location at which the product changes owners
hip must be agreed upon between the economic agents. For example, it
might be agreed that the product changes ownership when it is delivered to
the customer, or when it is accepted by the courier service. It might also be
agreed that the product changes ownership when it is picked by the cust
omer at the vendor’s premises. For the payment it is usually assumed that
money changes ownership when it arrives in the recipient’s bank account,
or when a check is deposited, but other agreements are also possible.

If the event of the sale is determined by its location of the product being
sold, the business application must have some information about the locat
ion (the LOCATION PATTERN must be configured on the product), or the
users of the business application must themselves determine whether the
sale has occurred.

Shipment Service

Creation or

Purchase of :

the Product is Product

not modeled «exchange process» Cash

Sales
Fig. 186. Value chain model for the sales process with shipment

The model in Fig. 187 shows the exchange Sales Process, and the conv
ersion Shipment Process. The output of the shipment process is Product;
this process changes one of its features — its location. The Shipment proc-


<!-- Page 298 -->


### 9.1 Sale and Shipment 293

ess has two inputs, Shipment Service, which is an economic resource that
the enterprise purchases from a Courier (the purchase is not modeled in
Fig. 187), and Product, which is used during shipment. The Product Use
decrement event indicates that the value of the product is for the enterprise
decreased during the shipment process. Indeed, the product, during shipm
ent, cannot usually be used, its amount can become smaller, and it can
get damaged. As shipment is a value-adding process, the enterprise expects
that the increase in the product value by changing its location is higher
than its decrease.

The model in Fig. 187 is not complete in the sense that it does not show
how the enterprise uses Cash, acquires Shipment Service (usually by an
exchange purchase process), and obtains Product (either by producing it in
a conversion process or buying it in an exchange process).

«conversion process» Shipment
«receive»
«economic agent» «economic agent»
Courier Enterprise
«provide» «receive» «provide» «provide»
«receive» «decrement» | aresourcen
Shipment «consume» Sheonent
Service ‘
«produce» Consumption Service
P «increment»
Shipment
«decrement»
«conversion x PUES
duality»
«resource»
Product use»
«exchange process» Sales
«outflow»
«exchange
«decrement» duality» «increment» | «inflow» | «resource»
Sales Cash Receipt Cash
i
«receive» ; «provide» TECEve®
«provide»
«economic agent» «economic agent»
Customer Enterprise
Fig. 187. The REA model for the sales process with shipment


<!-- Page 299 -->


294 9 Value Chains with Exchange and Conversion Processes


### 9.2 Resources Consumed During the Sales Process


Pi o_O ad

vatee® Joe. ue

Products usually do not sell themselves. Many companies have designated
personnel, such salesmen, who are responsible for sales, and make the
sales events occur. In many cases users of business applications would like
to track the salesmen’s labor and relate it to the sales events.

In addition to salesmen’s labor, selling products consumes other res
ources of the enterprise, such as the area where the products are on disp
lay, and others. We omit these additional resources from the model for
simplicity, and formulate the problem as follows.

Problem

How is the labor of the salesmen related to the sales economic events?
Solution

To solve this problem, we must realize that selling a product encompasses
exchange and conversion. The sales process is an exchange process bet
ween enterprise and customer. Parallel to this exchange process is a conv
ersion process that consumes salesmen’s labor and other resources in ord
er to make the sales events happen. For clarity, we also add the labor
acquisition process explaining how the enterprise gets rights to use the
salesmen’s labor. The value chain model is illustrated in Fig. 188. The
REA model for these three processes is illustrated in Fig. 189.


<!-- Page 300 -->


### 9.2 Resources Consumed During the Sales Process 295

«exchange process»
Labor
Sales

Creation or | Co

purchase of >> Product

Product is not roduc

modeled «exchange process» Cash
Sales as

Fig. 188. Value chain model for sales process with salesmen labor

The key point to understanding the model in Fig. 189 is to realize that
the enterprise acquires Labor from Salesman by the Labor Acquisition
event, and consumes this Labor to sell the Product. These three entities are
shown in bold in Fig. 189.

The conversion Sales Process consumes salesmen’s Labor, and changes
a feature of the product to “Is Sold”; this is equivalent to creating an ins
tance of the outflow relationship between the Product and the Sale event.

This model enables tracking labor of all human resources that are cons
umed in the sales process for each individual sale. For example, we can
extend the model by including the labor of warehouse personnel, cashiers,
etc., and other resources, such as their expenses, during the sales process.

The product is used during the Sales Process by the decrement event
Product Use. This event represents, for example, the time the Product is
on display on a shelf in a supermarket, or some other event that increases
the cost of the product in its relation to Sale.


<!-- Page 301 -->


296 9 Value Chains with Exchange and Conversion Processes
«exchange process» Labor Acquisition
«economic agent» ; «economic agent»
Salesman «provide» Enterprise
«receive» «provide» «receive»
aresources «outflow»| «decrement» «increment»
Cash : Cash «exchange» Labor
Disbursement Acquisition ‘anflows
«conversion process» Sales
«economic agent» «receivey | “economic agent»
Salesman Enterprise
ara «provide» «receive» «provide»
| «providen| «resource»
P Labor
: «decrement»
«increment»
Sale Labor
Consumption
ga > «consume»
«produce»| | Saleas [K
conversion | «conversion duality»
«decrement»
Product Use
“«use»
«resource»
Product
ROUIOWN «exchange process» Sales
«decrement» | «exchange»| «increment» ee «resource»
Sale Cash Receipt Cash
a
exchange ; . «receive»
«recelve” «provide» «provide»
«economic agent» «economic agent»
Customer Enterprise
Fig. 189. The REA model for the sales process with salesmen labor


<!-- Page 302 -->


### 9.3 People Management 297


### 9.3 People Management

Unless the enterprise is a one person company, the labor of economic
agents that work for the enterprise must be coordinated. Companies use
designated resources, managers, to be responsible for and to coordinate lab
or of subordinates. The labor of employees who are not managers is cons
umed to increase the value of a product. What is managers’ labor used
for?
Problem
What does the enterprise receive in return for its consumption of work of
managers?
Solution
The enterprise receives more efficient labor from the manager subordin
ates. Manager labor is consumed in order to increase value of subordinate
labor. Simply, the enterprise perceives the managed labor as having higher
value than non-managed labor. The value chain model is illustrated in
Fig. 190. The value chain consists of two processes: the labor acquisition
process, in which the enterprise acquires both managing and managed lab
or, and the people management process, in which the enterprise consumes
managing labor in order to increase the value of managed labor.


<!-- Page 303 -->


298 9 Value Chains with Exchange and Conversion Processes
«exchange process»
Cash Labor Acquisition
Labor
«conversion process» =
People Management
Fig. 190. Value chain for people management
The REA model for people management is illustrated in Fig. 191. In the
Labor Acquisition process the enterprise acquires Labor in exchange for
Cash. In the People Management process the enterprise consumes the
managing Labor during economic event Consume Labor, and the dual
economic event People Management increases the value of managed Lab
or.
«exchange process» Labor Acquisition
«economic agent» «economic agent»
Employee Enterprise
«receive» «provide» Rrsiities «receive»
Le
«outflow»| «decrement» «increment»
«resource» Cash «exchange» Labor
Cash ‘1 eh
Disbursement Acquisition .
«inflow»
«coversion process» People Management
consume «resource»
Labor
«decrement» |«conversion»| «increment»
Consume Labor reene
Management «produce»
«provide» «provide»
Secs «receive»
«economic agent»
Employee
Fig. 191. The REA model for people management


<!-- Page 304 -->


### 9.3 People Management 299

What if the users of a business application would like to impose the
business rule that only the labor of managers can be used to manage labor?

This can be achieved in two ways:

e Application designers can create a new economic resource, Manager
Labor, and relate it to the Consume Labor economic event. This solut
ion has the disadvantage that any change in this policy would require
change in the application design.

e Application designers can create a new labor type, for example, Mana
ger Labor, and create a policy that specifies that only the labor of
Manager Labor type is allowed to be consumed by the Consume Labor
event. This solution is more flexible, as the users of business applicat
ions can themselves change this policy, and also decide what to do if
the policy is violated.


<!-- Page 305 -->


300 9 Value Chains with Exchange and Conversion Processes

### 9.4 Education

= m4
; pau m
PN
ae
Many companies provide education to employees. The education process
creates costs for the enterprise. When creating an REA model for the educ
ation process, an application designer must answer the following quest
ion.
Problem
What does the enterprise receive in return for providing education to emp
loyees?
Solution
Through the education process, the enterprise hopes that it will receive
more efficient labor from its employees. Education is a resource consumed
in order to increase the value of Labor. The value chain model is illust
rated in Fig. 192. The value chain consists of two processes: the Educat
ion Acquisition process and the Learning process, in which the enterprise
uses the Education resource in order to increase the value of Labor.


<!-- Page 306 -->


### 9.4 Education 301

ee
Education
Acquisition
Education
Learning Labor
=
Employment
Fig. 192. Value chain for education

The REA model for education is illustrated in Fig. 193. This model is
from the perspective of a person receiving the education, for example, an
employee. Therefore, the agent Employee plays the role of the enterprise.
First, the Employee receives the Education in the Education Acquisition
process. This is an exchange process; note that in this example we make
the Employer pay for the education. Education is an economic resource
that the Employee uses to increase value of his Labor. The Employee also
uses his Labor to improve it, both by using his time for learning, and in
“on the job training”. In the Employment process, the employee sells his
labor to the Employer for Cash.

The economic resource Education is a permanent (not transient) res
ource; it is difficult or nearly impossible to erase the knowledge an emp
loyee receives from education. Therefore, the Education resource is used
and not consumed during the Learning Process.


<!-- Page 307 -->


302 9 Value Chains with Exchange and Conversion Processes
«exchange process» Education Acquisition
. F «economic agent»
«economic agent» «economic agent»
‘ Employee
Employer Education Provider .
(Enterprise)
«provide» «receive» «provide» «receive»
«resourcey | «outflow» «decrement» | «exchange» «increment»
Cash Cash Education
Disbursement Acquisition «inflow»
«coversion process» Learning
OS «resource»
Education
«decrement» ,
Use Education «conversion»
«increment»
«decrement» Labor Creation
Use Labor «produce»
«use»
«receive» «provide» «provide» enrovides
«receive» «economic agent» : «resource»
Employee «receive» Labor
(Enterprise)
«exchange process» Employment
«outflow»
i exchange
aresourcen | “IMflow») decrement» | “X°"8"9°| increment»
Cash Cash Receipt Labor Sales
! ——
«receive» «provide» «provide» «receive»
«economic agent» .
«economic agent»
Employee Employer
(Enterprise) Ploy
Fig. 193. The REA model for education (from the employee perspective)


<!-- Page 308 -->


### 9.5 Taxes 303


### 9.5 Taxes

We will illustrate the REA model for taxes on the example for value-added
tax (VAT); a similar model can be applied also for other fees to the gove
rnment. Paying taxes is the outflow of an economic resource, cash. This
outflow must be related to some inflow economic event according to the
REA domain rules. The usual problem in creating an application REA
model for taxes is formulated below.
Problem
What does an economic agent receive in return for paying taxes?
Solution
Although it might not be obvious at first, the enterprise often receives cert
ain services from the government in return for paying taxes. Public roads,
a legal system, and public security are examples of the benefits that the
government provides to the enterprise from collected taxes. We can also
consider the government as one of the vendors of the enterprise.

The value chain model for tax payment is illustrated in Fig. 194. The tax
payment process is an exchange of cash for government services. The gove
rnment services are then consumed in some value-adding process whose
result is the sales process.

The model in Figs. 194 and 195 represents the theoretical REA model
that explains tax. We will simplify the model in Fig. 196, and give an exa
mple of a model for VAT. VAT is used in European countries, and is
equivalent to the sales tax used in the US.

Is tax payment a value-adding process? As paying taxes is often not a
question of voluntary choice, the reason the enterprise pays taxes can be


<!-- Page 309 -->


304 9 Value Chains with Exchange and Conversion Processes
explained by fact that in doing so it avoids a potential penalty, rather than
by the fact that it perceives the value of the received services as higher
than the value of the cash paid.
Tax Payment
Government
Services
ee
Cash Some Value-Adding
Process >
Product
«exchange process»
a
Fig. 194. Value chain model for taxes

The REA model illustrating the theoretical solution explaining tax is in
Fig. 195. Government Services is an economic resource, consumed in
Some Value-Adding Process, a process adding value to Product, which is
then sold to customers in the Sales process.

The first problem with the theoretical solution in Fig. 195 is that we
cannot in practice determine which services from the government the ent
erprise uses, and the specific amount of these services.

However, we know the exact price for these services. The price is tax.
Therefore, the economic resource Government Services has an attribute
Owed Tax, which is the price of the Government Services acquired. Gove
rnments specify in their legislation a procedure to determine its value; this
legislation can be considered as a contract between the Government and
the Enterprise. For example, in Denmark this price is calculated as 25% of
the added value.

The second problem in the solution in Fig. 195 is that we do not know
how the enterprise consumes the Government Services and at what time.
Therefore, we simplify the model by omitting the conversion process, and
make an assumption that Government Services are reflected directly in the
price of the sold products, see Fig. 196. This is the same assumption as that
in the legal system of most countries.


<!-- Page 310 -->


### 9.5 Taxes 305

«exchange process» Tax Payment
«economic agent» «economic agent»
Government Enterprise
Kjecaiven «provide» «provide» «receive»
ds
‘fl «aecrement»
«resource | “QUNTOW) increment» pe aneee Receipt of
Cash Tax Payment 9 Government .
Services «inflow»
«coversion process» Consumption of Government Services
«resource»
Government
KCOonsSumMe» A 5
«decrement» |«conversion»| «increment» Services
Services Product
Consumption Creation
-—————— «produce»
«receive» «provide» ; «receive»
. «provide»
«inflow»
«economic agent» «economic agent» «resource»
Enterprise Enterprise Product
«exchange process» Sales
«outflow»
«decrement» «provide» | «increment»
Cash Receipt Sale
«provide» «receive» RIOCEING® «provide»
«economic agent» «economic agent»
Customer Enterprise
Fig. 195. Theoretical REA model explaining what tax is
In the model in Fig. 196, the Sales Process contains two decrement
economic events, Sale and Government Services. Both events usually app
ear on the materialized claim, such as a receipt from a shop or an invoice,
which often specifies the price of the product and the tax. If the Sales
Process occurs before the Tax Payment process, the value of the resource
Government Services, i.e. the property Owed Tax is negative.


<!-- Page 311 -->


306 9 Value Chains with Exchange and Conversion Processes
«exchange process» Tax Payment
«economic agent» «economic agent»
Government Enterprise
«receive» «provide» «provide» «receive»
_____
«outflow» «increment»
«resource» «decrement» Receipt of
Cash Tax Payment exchange? Government
Services «inflow»
«exchange process» Sales
This appears as IN «resource»
tax on the invoice | «outflow» | Government
- “~~ «decrement» Services
OW Government
; «account»
«exchange» Services
«increment» «decrement»
Cash Receipt Sale
«outflow»
«receive» ’ «provide»
«provide» «receive» «receive» «provide» «resource»
Product
«economic agent» «economic agent»
Customer Enterprise
Fig. 196. A simplified model for tax, with use of government services omitted
VAT is usually determined from the difference between purchases and
sales. An assumption in calculating the price of government services is that
sales increase the price of government services, and purchases of products
that contain VAT decrease the price of government services. This model
is illustrated in Fig. 197.


<!-- Page 312 -->


### 9.5 Taxes 307

«exchange process» Tax Payment
«economic agent» «economic agent»
Government Enterprise
«receive» «provide» «provide» «receive»
—_—____J
«outflow» «increment»
«resource» «decrement» «exchange» Receipt of
Cash Tax Payment Government
Services «inflow»
ainflow» «exchange process» Sales Process
IN
Me see as ex on «resource»
e invoice tocustomer |___
~~~ «decrement» — | «outflow» Government
Government SIvICgS
«exchange» Services «account»
< Ss Owed Tax
«increment» «decrement»
Cash Receipt Sale
i «provide» 5 «provide» : «outflow»
«receive» «P eT aeBiveD «receiven “P «provide»
«economic agent» «economic agent»
Customer Enterprise
«exchange process» Purchase Process
«outflow» «inflow»
«resource»
«increment» Product
«exchange» aa
S «increment» Ege)
«decrement» Govemment
Cash Receipt .
Services a
«receiver | I L «provide» oe
«provide» «receive» «receive» «provide»
This appears as tax [
«economic agent» «economic agent» on “ Ua Ieioseroee
Enterprise Vendor vendor
Fig. 197. The REA model for tax with purchase and sales


<!-- Page 313 -->


308 9 Value Chains with Exchange and Conversion Processes
How to determine the price for government services depends on national
legislation. For example in Denmark, VAT is calculated as a percentage of
the invoiced amount of specified products. In Germany and Sweden, VAT
is calculated as a percentage of the cash received. In the US, sales tax is
calculated as a percentage value of sale. Fig. 198 shows the sales process
where these rules are illustrated by dashed lines. A VALUE PATTERN can
be used to represent the tax amount.
«exchange process» Sales
«Claim»
Invoice «materialization»
etx)
Tax
| Value <= --}. «resource»
————e Government
«settlement» eee ‘ Services
Invoiced Amount _| | ;
value. emetetializanone
\
ES |
|
\ 5 f ' «outflow»
\ «aecremenb»
Government 4
Selvices
Denmark F
ccc
«exchange» ‘~__# -
Cash <sS oS «resource»
i Product
«inflow» «increment» — «decrement»
Lo Cash Receipt U S$ A Sale
ot «outflow»
Received Cash } Value of Sale _||
7
ec fere
Lo [Ld
«receive» «receive»
——-«receive»- eee «provide»
«provide» P
«economic agent» «economic agent»
Customer Enterprise
Fig. 198. VALUE PATTERN can be used to represent tax amount
Fig. 199 illustrates in more detail the Tax Payment process. The enterp
rise usually pays VAT periodically, typically several times a year. At the


<!-- Page 314 -->


### 9.5 Taxes 309

end of each period, the enterprise determines the cost of government serv
ices received. It does so by creating an instance of the economic event
Receipt of Government Services with a value that corresponds to the value
of the Government Services resource. This economic event creates a claim
between the Government and the enterprise. The enterprise often materiali
zes this claim by creating a document called VAT Settlement. In this
document, the enterprise indicates the amount of Received Government
Services in a given period. As the materialized claim is essentially a report,
it usually also indicates other information that the tax authorities require,
such as the amount of sales, and purchases, and the percentage of sales
VAT and purchase VAT. The company pays the amount due to the tax aut
horities; this Tax Payment settles the claim.

It can happen that the amount of taxes from purchase processes events is
higher than the amount of taxes from sales processes. In this case, the econ
omic event Receipt of Government Services has negative value, and the
enterprise receives Cash from the government.

«exchange process» Tax Payment
Government Enterprise
«receive» «provide» «provide» «receive»
dt
«decrement»
«outflow» «increment» Receipt of «inflow»

Services

«settlement» «materialization»
Government
VAT Settlement
Fig. 199. Payment of VAT to tax authorities

In many countries, the amount of tax depends on several factors, for exa
mple, on whether a customer is domestic or international, and there can
be different percentages of tax for different groups of products. The
POLICY PATTERN can be used to determine the tax value.


<!-- Page 315 -->


310 9 Value Chains with Exchange and Conversion Processes


### 9.6 Marketing and Advertising


= £& bac

po Oi a if

ay \ 5

a ) ’ il H o -

x | Mere ” “= 1

L i+ " Ca |

La -/ >) | > — i ‘ =e
ie Oh ue -
The purpose of marketing is to increase product awareness. Marketing
consists of many activities, including advertising; we will use advertising
as an example, that can easily be applied to other marketing activities. For
example, a company can place its advertisements on billboards, and pay
the advertising agency for the rented billboards. We will create an REA
model for an enterprise that buys advertising services.

Problem
To create a complete REA model, we need to answer the following quest
ion: What does the enterprise receive in return for the advertisements?
Solution
The motivation for advertisements is more sales of the products of the ent
erprise, although advertisements are often targeted to product types, rather
than to actual product instances. Advertising increases the cost of the
products; but the enterprise expects that it increases their value for the cust
omers, and, consequently, leads to more sales. Advertising creates an econ
omic resource, Product Awareness, which can be used to change one of a
product’s characteristics, namely, whether the product is commercially
known.

The solution consists of three business processes, the Advertisement Acq
uisition exchange process, in which the enterprise acquires an Advertising
Service from the agency, the Advertisement Service Consumption convers
ion process, in which the enterprise transforms the Advertising Service
into a Product Awareness, which is used to Make Product Known. The
known product is then sold in the Sales process.


<!-- Page 316 -->


### 9.6 Marketing and Advertising 311

«exchange process»
Advertising Service
Acquisition
Advertising
Service -
«conversion process»
Consumption
Product
: Awareness
«conversion process»
V Making Product
Product. ———>"' Known
«exchange process»
Sales
Fig. 200. Value chain model for advertising

During the Advertisement Acquisition process, the enterprise acquires
the Advertising Service and gives Cash to the advertising company in ret
urn. In our example, the Advertising Service is the right to use the billb
oard for a period of time. It could be a column in a newspaper or a slot on
TV. The contract with the agency specifies the details about this exchange,
such as the advertising media and the payment terms. After the Advertisem
ent Acquisition process, the Advertising Service resource is under the
control of the enterprise.

During the Advertisement Service Consumption process, the enterprise
consumes the Advertising Service. This event occurs during the time per
iod in which the enterprise had rights to use the billboard, the commercial
slot on TV, or in the moment of publishing the column in the newspaper.
This event is paired through a conversion duality with the economic event
Create Awareness, which occurs during the time period in which potential
customers see the advertisement. The awareness is often related to a produ
ct type; the enterprise owns an intangible economic resource Product
Type Awareness. In the conversion process Making Product Attractive the
enterprise uses the Product Type Awareness to increase the value of the
actual Product. The actual Product is then sold in the Sales process. The
solution is illustrated in Fig. 201.


<!-- Page 317 -->


312 9 Value Chains with Exchange and Conversion Processes
«exchange process» Advertising Service Acquisition
«inflow» «exchange «outflow»
«increment» :
Advertising | ality | «decrement»
Service _, vas
«resource» Acquisition Disbursement « >
Advertising resource
Service as
Enterprise has the rights to use billboard forl\
a period of time, or the spot in the news.
«conversion process» Advertising Service Consumption
«consume» ;
ccecie ents te «increment» «produce»
Advertising y Create
Service 7 Awareness «resource»
Consumption | ~~.
a Product Type
a Awareness
The advertisement is [\
published.
Obtaining Product Neconveralery process» Making Product Known «use»
is not modeled
L «increment»
«decrement»
«resource» Use Awareness Bate Precuct
Product Known
d i «conversion
«qgecrement» duality»
Suse” Use Product
«produce
«exchange process» Sales
«outflow» «exchange «inflow»
«decrement» duality» «increment»
Sale Cash Receipt
Fig. 201. The REA model for advertising


<!-- Page 318 -->


### 9.7 Waste 313


### 9.7 Waste

- J i ¥ mlx bed |
2 7 | ~ =
ag Pay e
“ iA |

The use of economic resources means that the resources decrease their
value, but still exist after decrement event. After some time the resources
can be used up so much that further use is impossible. For example, disc
harged batteries cannot be used for their original purpose. Such resources
can be considered as waste. However, when constructing the REA applicat
ion model, the application designer must answer the following question.
Problem
What does the enterprise receive in return for disposed waste?
Solution
The disposal of waste must be a value-adding process; otherwise, a rat
ional enterprise would not perform it. The disposal of waste consumes an
enterprise’s resources. For example, for the disposal of dangerous waste,
an enterprise usually pays a recycling company. Therefore, the disposal
event must be an increment economic event. As the disposal process is a
value-adding process, the value of waste must be negative at the disposal.


<!-- Page 319 -->


314 9 Value Chains with Exchange and Conversion Processes
=
added value. Tool Purchase
physically goes the
opposite way. : Tool Cash
Disposal
Fig. 202. Value chain for tool lifecycle including item disposal

Fig. 202 illustrates a value chain model of the lifecycle of an economic
resource Tool, including tool disposal. The model contains three processes,
Purchase, Production (in which the Tool is used), and Disposal (in which
the Tool is disposed). The Disposal process is a value-adding process,
which increases value of the Tool from some negative value to zero, by
giving it to the Recycling Company. The corresponding REA application
model is shown in Fig. 203.

For the entrepreneurial goals of the enterprise, the value of the Tool res
ource is negative at the time of disposal, and higher in absolute value than
the value of Cash given to the Recycling Company in return. The Disposal
event increases the value of the Tool from a negative value to zero. Theref
ore, Disposal is an increment event. It is paired through an exchange duali
ty to the Cash Disbursement event, because the enterprise pays the Recyc
ling Company for receiving rights and responsibility of the Tool.


<!-- Page 320 -->


### 9.7 Waste 315

«exchange process» Purchase
«economic agent» «economic agent»
Vendor Enterprise
«receive» «provide» — «provide» ¢receivey
«outflow»
: «decrement»
«increment»
Purchase cash
Disbursement «resource»
«exchange» Cash
«inflow»
«conversion process» Production
«economic agent» «economic agent»
«resource» Worker Warehouse Clerk
Tool
«receive» «provide» «provide» ¢receivey | «resource»
———S} Product
«use» =
«decrement» «increment»
«conversion» i «outflow»
Tool Usage Production Run
«exchange process» Disposal
«inflow» «outflow»
«increment» | «exchange» | decrement»
i Disposal Cash
: Disbursement
Toor Tete IN i — «provide»
negative value at AISceWwe? | «provide» p
the time of «receive»
disposal «economic agent» .
. «economic agent»
Recycling ‘
Enterprise
Company
Fig. 203. The REA model for tool lifecycle including disposal


<!-- Page 321 -->


316 9 Value Chains with Exchange and Conversion Processes

### 9.8 Purchasing and Selling Services


——aT

= _
#

_
Companies do not often perform all business processes using or consumi
ng their own resources; but some business processes are purchased from
subcontractors. In the REA framework, the economic agents cannot buy or
sell business processes, but only economic resources.
Problem
How do we model outsourced business processes?
Solution
If an economic agent performs a business process for another economic
agent, the economic agents exchange a service. A service is an economic
resource resulting from a business process performed by an economic
agent for another economic agent. Services are transient resources that are
consumed at the same time as they are created.

As an example, we will illustrate a model in which the enterprise purc
hases the inspection service from a vendor. The value chain model is
shown in Fig. 204.


<!-- Page 322 -->


### 9.8 Purchasing and Selling Services 317

Part «conversion process»
Tool Assembly
Product
«conversion process»
Quality Inspection
Inspection Service
«exchange process»
Inspection Service Cash
Purchase
Fig. 204. Inspection service exchange
Fig. 205 illustrates the model from the perspective of the provider of the
quality inspection service, and Fig. 206 illustrates it from the perspective
of the recipient of quality inspection service.
«exchange process» Inspection Service Sale
«exchange
“Quali duality» «increment» «inflow» | «resource»
: Cash Recei Cash
«outflow» Inspection pt
«conversion process» Produce Inspection Service
«resource»
Inspection .
Service «conversion
«increment» duality» «decrement» /«consume>|
«produce» Inspection Labor ‘ “\abor »
Service Creation Consumption
Fig. 205. The REA model for outsourced inspection, inspection provider view


<!-- Page 323 -->


318 9 Value Chains with Exchange and Conversion Processes
«conversion process» Assembly
«resource» | “CONSUME! — ¢decrement» «increment»
Part Material Issue Assembly
«resource» seca «decrement» «conversion soduce” «resource»
Tool Tools Usage duality» Product
«conversion process» Inspection
Resource is Nee sun” Change the quality IN
used by group of the product |__
inspection «decrement» «produce»
Material Issue F
«conversion
duality»
«decrement»
«resource» | «consume» Inspection -_ «increment»
Inspection Service Inspection
Consumption
«inflow» «exchange process» Inspection Service Purchase
«increment» «decrement» «CONSUME»! CL acourcen
Inspection Cash Cash
Service Creation | “&Xchange) Disbursement
duality»
Fig. 206. The REA model for outsourced inspection; inspection recipient view
In Fig. 206, the economic resource Inspection Service is purchased from
a Vendor in exchange for Cash. The Inspection Service is consumed by the
decrement event Inspection Service Consumption, which is paired through
a conversion duality with the increment event Inspection.


<!-- Page 324 -->


### 9.9 Transient Resources 319


### 9.9 Transient Resources

Some resources are consumed at the same time they are created. They cann
ot be stored, and the enterprise cannot put them on stock because of their
physical nature. Electricity is an example of a transient resource that we
came across earlier. Other examples are services the enterprise receives or
provides. The fact that a resource is transient does not change any of the
REA modeling principles.
Electricity
An enterprise receives electricity from an electricity distributor. The enterp
rise consumes electricity for heating buildings, running machines, supp
orting its infrastructure, and many other things. Suppose for simplicity
that the enterprise consumes electricity only for heating. The model can
easily be extended to cover other uses of electricity.

For clarity, we will make a model from two perspectives, that of an
electricity provider and of an electricity consumer. A simplified model for
an electricity provider is illustrated in Figs. 207 and 208.

; Fuel
«conversion process»
Electricity Production Generator
Distribution
Electricity Network
«exchange process»
Fig. 207. Business processes for an electricity provider


<!-- Page 325 -->


320 9 Value Chains with Exchange and Conversion Processes
«conversion process» Electricity Production
«resource ie»
Electricity ee «decrement» «use» «resource»
Generator Use Generator
«specification» «conversion»
«produce» Sy) «consume» «resource»
S «increment» Fuel Fuel
Electricity Consumption
Production
«resource» «decrement» «use» «resource»
Electrici Distribution Distribution
ty Network Use Network
“outflows «exchange process» Sales
«decrement | «exchange» . b> «inflow»
Delivery of «increment «resource»
Electricity Cash Receipt Cash
Fig. 208. The REA model for an electricity provider
Models for an electricity consumer are illustrated in Figs 209 and 210.
The electricity consumer purchases electricity, i.e., the consumer exc
hanges Cash for Electricity. The consumer produces heating by using Rad
iator and by consuming Electricity.
«exchange process»
Electricity Purchase Cash
Electricity
«conversion process» .
Radiator Production of Heating Heating
Fig. 209. Value chain from electricity consumer’s viewpoint


<!-- Page 326 -->


### 9.9 Transient Resources 321

«exchange process» Purchase
«resource»
«resource type» | | frequency range Cash
Electricity Type
tgs ] «outflow»
«specification» «increment» ——|«exchange»| «decrement»
Electricity Cash
Purchase Disbursement
«inflow»
«conversion process» Heating Production
Tlectriaty. {77 LActual voltage and frequency Ds
«decrement» «increment»
econsume), Electricity Heating
Consumption Production
_ «produce»
«resource» SUSE” «decrement» «conversion»
Radiator Radiator Use «resource»
Heating
Fig. 210. The REA model for an electricity consumer

Electricity is a transient resource. The events Electricity Receipt and
Electricity Consumption occur simultaneously, and the resource Electricity
is consumed at the same time it is produced. Heating is also a transient res
ource; the event Consuming Heating is omitted from the model for simp
licity.

Electricity Instance and Modeling Compromise

Please note that Electricity in Fig. 210 is a resource instance. While the res
ource type Electricity Type is at runtime characterized by frequency range
of 50 to 60 Hz, voltage from 220 to 230 V, and current from 0 to 20 A at
any given time, the resource instance Electricity is at runtime characterized
by actual values of frequency, voltage, and current. The implementation of
such resource instance requires an array or similar data structures to store
the values in all moments in time relevant for the users of business applicat
ion.

The management of many electricity consumers is not interested in data
stored in the electricity instance resource. Average electricity consumers
are interested only in the total amount of electricity delivered, and this can
be obtained from an account (see the ACCOUNT PATTERN for details) on
Electricity Type. Therefore, a simpler and more convenient model would


<!-- Page 327 -->


322 9 Value Chains with Exchange and Conversion Processes
include a modeling compromise omitting the resource instance electricity
and the connecting economic events Electricity Purchase and Electricity
Consumption by a consume relationship with resource type Electricity
Type. We must be aware that this is a modeling compromise and we lose
some business information in order to obtain a simpler model.
«exchange process» Purchase
Common modeling compromise. The |X. éresource»
model does not track information about Cash
actual voltage, frequency and current.
- s : «increment» |«exchange»| «decrement» «outflow»
LE Electricity Cash
Purchase Disbursement
«resource type»
Electricity Type
«conversion process» Heating Production
«consume»
«decrement» «increment»
Electricity Heating
Consumption Production «produce»
«resource» <USe> «decrement» > «resource»
Radiator Radiator Use «conversion» Heating
Fig. 211. Modeling compromise of the REA model for an electricity consumer


<!-- Page 328 -->


10 Processes with Contracts

In this section, we illustrate examples of REA models at the policy level.
These models determine what should or could happen, as opposed to what
has happened, which is the purpose of the models at the operational level.

The models for PURCHASE ORDER and LABOR ACQUISITION illust
rate typical contracts that are part of most business applications.
GUARANTEE and INSURANCE are contracts might not look like typical
exchanges of economic resources; therefore, we will illustrate how their
REA models look.

When economic agents sign a contract, they usually expect that both
partners will fulfill their commitments. However, this does not happen alw
ays, and we will illustrate it in the section PENALTY FOR NOT
FULLFILLED COMMITMENTS. PRODUCTION SCHEDULE is similar
to contract, but covers commitments for conversion processes. The REA
model for TRANSPORT illustrates how the contract and schedule are rel
ated.


<!-- Page 329 -->


324 10 Processes with Contracts

### 10.1 Purchase Order

atl ;*
ee.

ad : > “4
In business to business and some business to customer scenarios, a cash
sale is rare. Usually, the enterprise places a purchase order for the produ
cts, the vendor sells the goods, and the enterprise eventually pays for the
goods. The purchase order is a business document that contains names of
economic agents, a date, a list of the ordered items, and, often, their prices
and other additional information.
Problem
How are the purchase order and its components represented in the REA
model?
Solution
The REA model does not give an answer to this question.’ We will present
one possible mapping of the REA entities to the components of the Purc
hase Order, though other mappings might exist as well.

The Purchase Order, see Fig. 212, is a contract between economic
agents Vendor and Enterprise. The purchase order lines, Purchase Lines,
are the commitments of the contract. For the enterprise, the Purchase Line
is a commitment to receive economic resource Product, and the Payment
Line is a commitment to pay for it. At runtime, the Purchase Order can
have several Purchase Lines — several products can be specified on one
Purchase Order, and several Payment Lines — the products can be paid for
in several installments or using different payment methods.

8 For many people studying REA, this is key information, helping them unders
tand the purpose and scope of the REA ontology.


<!-- Page 330 -->


### 10.1 Purchase Order 325


A Purchase Line can be related to a Product Type (in the case the enterp
rise orders a product in a catalogue), but, eventually, by the time of the
Purchase, it must be related to an actual Product. A Payment Line can be
related to a Cash Type, specifying the payment method, but, eventually, by
the time of Payment, it must be related to an actual economic resource
Cash.

Companies often materialize the claim and create an invoice, which is
used to inform the economic agents about the value of the imbalanced dua
lity.

The model in Fig. 212 illustrates the REA model for Purchase Order,
and also the economic events that fulfill the commitments; so the model
contains all REA entities needed to model the expected path of the purc
hase process. The model in Fig. 212 does not contain invoice, as in a
world where information is passed electronically, this document is not act
ually necessary to successfully conduct business; the economic agents
have all the information they need in the contract.

«agent» «party» sore «party» «agent»
Vendor seller Order buyer | Enterprise
| «comitted receive» «comitted receive»
ee, es
«comitted provide» «comitted provide»
«clause» «clause»
«resource altign «decrement | «exchange xoutliaw
type» reservation») Gommitment» |reciprocity» icecrement reservation» «resource
Predict Puneiass Payment Line Cash Type
Type Line
«inflow «outflow
«specification» Teservation» — «fulfillment» «fulfillment» reservation» «Specification»
«exchange
«resource» ; «decrement» | duality» | «increment» «resource»
Product «inflow» Purchase Payment | «outflow» Cash
«provide» ———__ «provide»
«receive» «receive»
«agent» «agent»
Vendor Enterprise
Fig. 212. Purchase with purchase order

Model in Fig. 213 is an instance model showing a Purchase Order,
No. S567, between Vendor C42 and the enterprise. The Purchase Order
has two commitments, the Purchase Line on two Product Types No. [23


<!-- Page 331 -->


326 10 Processes with Contracts

and the price agreed upon for these two products (the Payment Line), $10.
The Payment Line commitment is fulfilled by two economic events, Purc
hase, each on an actual Product. The Payment Line commitment is fulf
illed by an event Payment of $10.

The purchase order number, the product type number and the product
serial number can be implemented using the IDENTIFICATION
PATTERN. The commitment Purchase Line contains information about
quantities of the resources and their prices. These properties can be imp
lemented using the VALUE PATTERN. The economic event Purchase
does not contain information about the quantities of the products, as at runt
ime every Purchase event represents the purchase of one unit of the
Product.

«agent» «party» «contract» «agent»
Vendor maa Purchase Order a Enterprise
No.= $567 Yer No=V45
«comitted receive» «clause» «clause» «comitted receive»
es
«comitted provide» «comitted provide»
«inflow

«resource | reservation»| «decremen «outflow | “resource

type» commitment» «exchange «decrement cseemetiars type»

Product Piirchasé reciprocity» commitment» Payment

Type Line Payment Line Method
No.= [23 ToPay=$10
ification» «inflow
«Speci r 10> eservation» sul . «outflow =
vox ot «inflow — «tullliment» «fulfillment» reservation» nk
ae | FeSeHation» ae «specification»
«resource» «decrement» z «resource»
«inflow» Payment
SerialNo.=12 || —— eso
Balance=$95
«receive»
«resource» «decrement»
Product Purchase
i «receive»
SerialNo.=13 [~ «inflow»
«provide» «provide» «receive» «provide»
«agent» «agent»
Vendor Enterprise
No.=¥45
Fig. 213. An instance model of a purchase order

The model in Fig. 213 is in some sense a minimal model that illustrates

one possible implementation of the REA application model. Users of busi-


<!-- Page 332 -->


### 10.1 Purchase Order 327

ness applications usually require much more functionality on the REA ent
ities, most of can be implemented using the behavioral patterns illustrated
in Part II of this book. Users usually tolerate many modeling compromises;
for example, many current business applications do not use the Payment
Line, but place its information on the Purchase Order. This compromise
does not allow, for example, for payments in several installments.


<!-- Page 333 -->


328 10 Processes with Contracts

### 10.2 Labor Acquisition

Df
Labor is one of the resources of an enterprise; in many businesses, such as
in information technology, labor is probably the most important resource.
Problem
How does the enterprise acquire labor?
Solution
The enterprise usually acquires labor in exchange for cash. At the operat
ional level, the labor acquisition process is similar to the purchase process.
The different forms of labor acquisition, such as employment, consultancy
services, etc., are modeled by different kinds of contracts, but the models
are similar at the operational level.
«exchange process» ~

Fig. 214. Labor acquisition

Labor acquisition with employment contract is illustrated in Fig. 215.
The economic resource Labor is a transient resource (it is consumed at the
same time it is created). The increment event Labor Acquisition specifies
the time interval in which the Enterprise acquires Labor from the Emp
loyee.


<!-- Page 334 -->


### 10.2 Labor Acquisition 329


The Employment commitment specifies the time interval in which the
Employee agrees to provide his Labor to the Enterprise. If the employment
is for unspecified time, the end of the Employment commitment is also uns
pecified. In such a case, the terms of the Employment Contract usually
specify a procedure or condition for the end of the Employment commitm
ent. The Employment commitment reserves Labor Type, which specifies
the kind of Labor the Employee is committed to providing, and often also
qualifies types of tasks. The commitment Salary specifies the amount of
Cash to be paid in exchange for the Labor.

Users of business applications usually decide to materialize the claim
between Labor Acquisition and Salary Payment, and print a report, somet
imes called Deposit Notification, with the details of the acquired Labor
and Cash paid, and send it to the Employee with the payment.

«agent» «party» «contract» «party» «agent»
Employee Employment Enterprise
employee Contract employer
«comitted receive» «Clause» «clause» «comitted receive»
es [oe
«comitted provide» es | «comitted provide»
«inflow «exchange «outflow
«resource reservation» | «decrement reciprocity» «decrement | reservation» ype
type» commitment» commitment» c P
ompensaL
abor Type Employment Salary .
tion Type
inflow «outflow
«specification» Teservation» — «fulfillment» «fulfillment» reservation» «Specification»
4 , «exchange ->— ‘
«gecrementl» duality» «increment»
resource:
“ Labor ” | Ginflow» Labor Salary «outflow» Cosh
Acquisition Payment
«provide» «provide»
«receive» «receive»
«agent» «agent»
Employee Enterprise
Fig. 215. Employment

An employment contract might specify multiple kinds of compensation
for the labor, such as salary and bonus. In these cases, the model will cont
ain several decrement commitments, such as Salary commitment and Bon
us commitment; they can be fulfilled by single or multiple salary payment
economic events.


<!-- Page 335 -->


330 10 Processes with Contracts

### 10.3 Guarantee

<<AlMe
—7 fe 3X
a
S eqeey
ARR
The guarantee is a promise by the provider that the economic resource
provided will either perform satisfactorily for a given length of time under
certain conditions, or the recipient will receive a specified compensation,
such as the repair or replacement of the product or the return of cash.
Problem
How do we create an REA application model for guarantee?
Solution
A guarantee is a term of the contract that instantiates an additional comm
itment under conditions specified in the contract. The value chain model
is illustrated in Fig. 216.
Product > Cas
«exchange process»

Fig. 216. The sales process that accepts product return

For example, a money-back guarantee is a promise by the seller to acc
ept the return of the product under certain conditions (such as the product
not having been used), within a limited period of time (for example, 30
days).


<!-- Page 336 -->


### 10.3 Guarantee 331

«resource» «resource»
Customer buyer seller Enterprise
«party»«party» (committed «committed
«committed provide» receive»
«committed ecelve» «contract» «committed
provide» «clause» Sales Order ere
«committed
«term» «clause» «clause» i
re ee ee oe type»
~~--fTT TSN Cash Type
N —_ N
\ 7 \
r , \ «inflow
«decrement } «increment , reservation»
r- commitment» | ; commitment»
«outflow Sale Cash Receipt
. \ \
reservation» «outflow \
reservation» «increment Re «decrement
— commitment» commitment» F
«specification»
Product Return «exchange Money Return P
. reciprocity»
«resource «fulfillment» «inflow
type» «fulfillment» «fulfillment» reservation»
Product Type «fulfillment»
infl «resource»
«outflow» | «decrement» «exchange! «increment» «intlow» Cash
Sale duality») Cash Receipt
«specification» So: «outflow»
«resource» ; ~
Product «inflow» «increment» «decrement»
Product Return Money Return «receive»
«receive» «provide» __| «provide» «provide»
«receive» «receive»
«agent»
Customer _ — | ——. «agent»
——— Enterprise
«provide» «materialization» «settlement»
«claim»
Invoice «settlement»
«materialization»
Sale Value
Cash Receipt Value
Product Return Value
Money Return Value
Balance Value
Fig. 217. The REA model for the sales process that accepts product return
An REA application model for money-back guarantee is specified in
Fig. 217. If the conditions of the guarantee are met, two commitments are


<!-- Page 337 -->


332 10 Processes with Contracts

instantiated; Product Return and Money Return. The commitments are fulf
illed by economic events Product Return and Money Return. The events
influence the value of the Claim that exists between the economic events
Sale, Cash Receipt, Product Return, and Money Return. If a customer dec
ides to return the product, and the enterprise accepts it and registers the
return, the Claim will be created and the Money Return event will settle the
claim.


<!-- Page 338 -->


### 10.4 Insurance 333


### 10.4 Insurance

sf

An insurance contract is a contract between two economic agents, in which
one agent (the insurer) agrees to reimburse another agent (the insured) in
the case of loss or harm of an insured economic resource, such as property
or life, in specified contingencies, such as fire, accident, and death, that
occur under the terms of the contract. The insured agent agrees to provide
a payment proportionate to the risk involved.

When making an REA model for an insurance contract, application dev
elopers have to answer the following question.
Problem
What does the insured economic agent receive in return for his payment?
Solution
In a sense, the insured economic agent receives security, but this is not the
correct answer. The insured economic agent receives reimbursement in
cash in the cases specified in the terms of the insurance contract. Theref
ore, the REA model at the operational level is a simple exchange of cash
for cash; see Fig. 218.

«exchange process»
ToD

Fig. 218. Insurance


<!-- Page 339 -->


334 10 Processes with Contracts
The insurance contract from the perspective of the insured enterprise, is
illustrated in Fig. 219. The contract contains the Cash Disbursement comm
itment, specifying the premium the enterprise pays the Insurer. The rec
iprocal Cash Receipt commitment is not instantiated when the contract is
signed, because the Insurer does not have to pay anything to the insured
enterprise unless there is loss of or harm to the insured resource. These
conditions are specified by the Insurance Policy, a term of the contract,
which can instantiate the Cash Receipt commitment.
«economic agent» «party» «party» «economic agent»
Insurer insurer insured Enterprise
«decrement» «clause» F
«receiver Insurance eer oclion «provide»
Contract nsurance Folicy ~, | ;
«provide» «clause» . _s «receive»
«clause» instantiate
«aecremen . !
. «increment 7
commitment» . y
n e
Disbursement reciprocity» Cash Receipt
«fulfillment» «fulfillment»
—————— | ——-x«reservation»n————_——_.
«reservation» _
«exchange
«resource» «decrement» duality» «increment»
Cash Cash Cash Receipt
«outflow»! Disbursement P
“ NK «inflow»
“a «materialization» «settlement» Value of this Cash
Payment of the IN Receipt is equal to the
insurance premium «claim» insured amount if
Insurance Claim covered by insurance
policy.
Fig. 219. Insurance contract


<!-- Page 340 -->


### 10.5 Penalty for Violated Commitment 335


### 10.5 Penalty for Violated Commitment

7" T.
e °@

A sales order contains commitments that represent promises of future econ
omic events that both contracting parties promise to fulfill. Contracts
usually also specify terms for what should happen if some of the commitm
ents are not fulfilled as promised. For example, it can be specified that an
economic agent that cannot fulfill a commitment has to pay a specified
penalty to the other economic agent. The promise to pay a penalty is not a
commitment when the contract is signed; it may become a commitment
under the conditions specified by the terms of the contract.

The payment of the penalty is an outflow of resources. To make a full
REA model that includes penalties for violated commitments, an applicat
ion designer must answer the following question
Problem
According to the REA rules, every resource outflow must be paired
through an exchange duality with some inflow. What does an economic
agent receive in return for a paid penalty?

Solution
The short answer is nothing, for the penalty as such, because a commitm
ent to pay a penalty for a violated commitment makes sense only when
considering the original commitment that has been violated. However, a
penalty reduces the value of the claim of the original exchange.

Prod «exchange process» => Cash

roduct Sales

Fig. 220. Sale with possible penalty payment


<!-- Page 341 -->


336 10 Processes with Contracts
«resource» «resource»
Customer buyer seller Enterprise
«party» «party» committed
«committed provide» «committed
«committed receive» «contract» e reoriven
. «commi
receive» «clause» Sales Order provides
«term» «clause» «clause»
Failure to Sell «resource
type»
S~~-foIL Cash Type
- _—
\
\ «exchange «inflow
«decrement \ reciprocity» «increment reservation»
- commitment» +! commitment» ———
«outflow Sale 1 < Cash Receipt
reservation» \ «committed
«outflow provide» |.
reservation» «decrement ss «inflow
—— commitment» reservation»
Penalty Payment —_
: «outflow .
«resource «fulfillment» reservation» «specifit
ype» «fulfillment» «fulfillment» cation»
Product Type
F «resource»
«outflow» | «decrement» | |«exchange | «increment» «inflow»
Sale duality» | Cash Receipt
«specification» x
«resource» «outflow»
Product «decrement» es ene
Penalty Payment «receive»
. «provide»
«receive» i
«receive» «provide»
agent —_I _ «agent»
ustomer — Enterprise
«provide» «settlement»
«materialization»
«materialization» Invoice
Sale Value
Cash Receipt Value
Penalty Value
Balance
Fig. 221. Contract with penalty for failure to sell
An REA model for a Sales Order with a penalty for failure to sell is ill
ustrated in Fig. 221. The contract term Failure to Sell specifies that the
Enterprise pays Cash as a penalty in the case where it fails to deliver (and


<!-- Page 342 -->


### 10.5 Penalty for Violated Commitment 337

consequently to sell) products in a specified time. If the condition in a cont
ract term becomes true, the Penalty Payment decrement commitment is
created, which can be fulfilled by the decrement event Penalty Payment.
The consequence of this economic event is that the difference in the Claim
between Sale and Cash Receipt is reduced by the value of the Penalty, so
this Claim can be settled by a Cash Receipt of less value; the original value
of the Claim is decreased by the value of the Penalty.

Note that, at runtime, the decrement commitment Penalty Payment is ins
tantiated by the Sales Order contract only if the conditions specified in
the Failure to Sell term are met. The Penalty Payment commitment is not
instantiated when the Sales Order is registered. An analogous model can
be made for a penalty for late payment.

The economic resource transferred as a penalty can be different from
Cash; it can, for example, be a product or a service.


<!-- Page 343 -->


338 10 Processes with Contracts

### 10.6 Schedule

Creating a product is seldom a spontaneous thing. Companies usually plan
and schedule the usage and consumption of their resources. The aim is to
optimize the usage and to fulfill the exchange commitments to other econ
omic agents.
Problem
How do we create an REA application model for a production schedule?
Solution
A production schedule consists of commitments to use, consume, and prod
uce economic resources. A value chain model for the product creation
process is illustrated in Fig. 222, and an REA application model for a prod
uction schedule is illustrated in Fig. 223.

Part

"S/“«conversion process»
Labor Assembly and Product
Inspection

Tool

Fig. 222. Value chain model for creating a new product
In Fig. 223, the Production Schedule consists of four commitments;

Material Requisition, Tools Requisition, and Labor Requisition are the
decrement commitments paired through a conversion reciprocity with the
increment commitment Production Order.


<!-- Page 344 -->


### 10.6 Schedule 339

«clause» «schedule»
Production
Schedule
«clause»
«clause» «clause»
«economic : : ;
«economic «economic «economic
agent» agent» agent» agent»
Warehouse . .
Supervisor Worker Supervisor
Clerk
«committed «committed |«committed
«committed |«committed provide» receive» provide» «committed
. . | receive»
provide» provide» .
«committed
| receive»
«consume
«resource reservation» «decrement «increment
type» —|—|- commitment» commitment»
Labor Type —|—|- Labor Requisition Production Order
«use «decremen qconversion
resource : . 5 nvers «produce
reservation» commanrenb» 7 reciprocity» reservation»
oo!
Tool Type | Requisition
«consume / «resource
: «decrement type»
— a
Material
fulfillment»
Part Type Requisition “ ent «produce «specification»
«CONSUME consume _ reservation»
«fulfillment»
reservation» -ocervation» «fulfillment» «resource»
| | Product
«resource» «consume» «decrement»
Part | Material Issue
«use «conversion «produce»
reservation» _~ duality»
«use» «increment»
«resource» ; «decrement» Assembly and
Tool : Tool Usage - Inspection
«decrement»
bor «consume» Labor ereceive»
| Consumption
«receive» F
. . «receive»
«provide»: . «receive»
«provide» «provide» |
«provide»
«economic «economic «economic
agent» agent» agent»
Warehouse Clerk Supervisor Worker
Fig. 223. The REA model for a production schedule and a production run
The Material Requisition commitment is a promise by a Warehouse
Clerk to make a specified amount of Part Types available to the Worker.


<!-- Page 345 -->


340 10 Processes with Contracts

The Tools Requisition commitment is a promise by the Warehouse Clerk
that tools of a specified Tool Type will be available to the Worker, and a
promise of by the Worker to deliver the tools back. The Labor Requisition
commitment is a promise by the Worker to the Supervisor to consume
worker’s Labor in a specified time. The Production Order commitment is
a promise by the Worker to the Supervisor to produce an instance of Produ
ct Type.

For simplicity, we have not illustrated in the model that Labor is a
specification of Labor Type, Tool is a specification of Tool Type and Part
is a specification of Part Type. At runtime, when the production schedule
is created, there usually are reservation relationships between the commitm
ents and the resource types (Part Type, Tool Type, Labor Type, and
Product Type), but eventually these commitments must also be related by
reservation relationships to the resources (Part, Tool, and Labor).

The commitments are fulfilled by economic events that record the actual
conversion process; for example, Material Requisition is fulfilled by Mater
ial Issue, Labor Requisition is fulfilled by Labor Consumption, and Prod
uction Order is fulfilled by Production Run.


<!-- Page 346 -->


### 10.7 Transport 341


### 10.7 Transport

|
\
ee
The following example illustrates an REA application model of a company
transporting its employee on a business trip. The employee is represented
as labor; the transport changes value of the labor for the enterprise, which
believes that the employee’s labor will be more worth at his destination loc
ation than at his original location. The transport is a service purchased
from a transportation provider. The enterprise consumes the transportation
service (in this example manifested as a seat on an airplane) in order to
move labor from one location to another. The value chain model for this
example is illustrated in Fig. 224.
«exchange process»
Seat
An object being eo Labor < «conversion process»
Fig. 224. Value chain model for transport

The solution consists of two processes. During the Transport Purchase
process, the enterprise receives the economic resource Seat in exchange
for Cash. In the Transport process, the enterprise consumes the resource
Seat to modify the value of Labor. The basic idea is that the employee’s
labor at the destination location has more value for the enterprise than at
his location of origin.

The REA model is illustrated in Fig. 225. The resource Seat represents
an actual place in the transport vehicle. The resource Seat Type specifies
some features of the seat, such as business or economy class, and window
or aisle. Please note that the specification of class, a window or aisle can


<!-- Page 347 -->


342 10 Processes with Contracts
be modeled as a Seat Type attribute, or as a relationship to the Seat Categ
ory group.

Sometimes, a Seat Reservation is for an actual seat in the transport vehic
le. In this case, the reservation relationship to Seat Type is omitted and the
reservation relationship is directed to the actual Seat instead of to Seat
Type.

The exchange process Seat Transport is governed by a Transport Cont
ract, consisting of clauses Seat Reservation and Cash Disbursement. The
commitment Seat Reservation specifies when the reservation took place
and the reservation terms (such as in what time interval the place can be
occupied). The commitment Cash Disbursement specifies the price for the
Seat Type, and, usually, also when the payment should occur. The econ
omic event Rights to the Seat specifies when, i.e., in what time period, an
actual seat has been sold.

The conversion process Transport is governed by Transport Schedule,
consisting of the clauses Seat Occupation and Move commitments. The
commitment Seat Occupation specifies when the employee is supposed to
use the Seat. Seat Occupation is a decrement commitment because during
transport Labor usage of Labor for its original purpose might be limited.
The economic event Seat Occupation might be different from Seat Reserv
ation; for example, there can be scheduled a one-way trip, but a reserved
return ticket, because the return flight is cheaper. The Move commitment
represents when the actual move of Labor takes place, as well as the locat
ions of origin and destination. The Move is an increment commitment,
because, at the end of Move, Labor has more value to the enterprise than at
the beginning of Move.


<!-- Page 348 -->


### 10.7 Transport 343

«exchange process» Transport Purchase
«contract»
Transport
Contract
«clause» «clause»
«reservation»
«exchange-—qecrement
«increment reciprocity») — commitment»
commitment» Cash «reservation»
«resource Seat Reservation Disbursement
type»
Seat Type «reservation» «resource»
ezpecitcation» afullfillment» Cash
P «fulfillment»
«outflow»
«inflow» eexchange d t
«resource» «increment» duality» ace Meny
Seat Right to the Seat woe
Disbursement
«consume» «conversion process» Transport
«conversion
«decrement» duality» «increment»
Seat Occupation Move «produce»
«fullfillment» «fullfillment»
«consume» «resource»
Labor
«reservation» F
r «conversion
. «decrement reciprocity» «increment .
«reservation» commitment» commitment» _ | «reservation
Seat Occupation Move «reservation»
«clause» «schedule» «clause»
Transport
Schedule
Fig. 225. The REA model for transport


<!-- Page 349 -->


Appendices


<!-- Page 350 -->


A. REA Ontology
“An ontology is a study of the categories of things that exist or may exist
in some domain” (Sowa 1999). Ontological categories define the concepts
that exist in the domain, as well as relationships between these concepts.
Geerts and McCarthy (Geerts, McCarthy 2000, 2002) formulated REA as
an ontology for business systems. The REA ontological categories are ill
ustrated in Fig. 226.
Contract /
Schedule
reservation
reservation ; ;
reciprocity
Resource Agent
link duality I
typificatio mae typification typification responsibility
Economic Economic Event Economic
Type Type Agent Type

ie characterization |
characterization characterization
Fig. 226. REA ontology

The purpose of this appendix is to outline the difference between the
model illustrated in Fig. 226 and the model we described in Part I of this


<!-- Page 351 -->


348 A. REA Ontology

book. We described in this book exchanges and conversions as separate
patterns, because the semantics of the modeling elements in exchanges and
conversions are different, although the models are structurally similar and
can be mapped to common concepts. Only the economic resource and poli
cy have the same semantics both in exchange and conversion, because
economic resources link the exchange and conversion processes, and a
single policy may be applied to some entities in an exchange process and
some entities in a conversion process.

The ontological categories not described in this book are stockflow, part
icipation, and characterization. Stockflow is a common concept for the inf
low, outflow, use, consume and produce relationships. Duality is a comm
on concept for the exchange duality and conversion duality.
Participation is a common concept for the provide and receive relations
hips. Characterization is a common concept for the linkage type, respons
ibility between agent groups and the custody between resource and agent
groups.

REA does not have explicit names for the relationship linking REA entit
ies to a Contract or Schedule; and linking REA entities to a Policy. The
reasons are beyond the scope of this book: Contract, Schedule and Policy
are mediating entities, also called “thirdness” in (Sowa 1999); therefore,
these relationships are not standalone ontological categories. Table 3 outl
ines intuitive meaning of the REA ontological categories.

Table 3. REA ontological categories
Concept Intuitive Meaning
Economic Resource A thing that users of business application want
to plan, monitor and control
Economic Resource Type _A type or group of economic resources
Economic Event Type A type or group of economic events
Economic Agent Type A type or group of economic agents
Economic Event
In exchange A moment or time interval during which rights
to an economic resource are transferred from
one economic agent to another
In conversion A time interval during which resources change
their features or existence, and economic agents
receive or lose (in the case of changed existence)
or transfer (in the case of changed features)
physical control over the resource
Economic Agent
In exchange A legal entity possessing rights to an economic
resource
In conversion A person having physical control over an eco-


<!-- Page 352 -->


A. REA Ontology 349
nomic resources
Characterization A relationship between economic resource type,
economic event type and economic agent type.
Responsibility
In exchange A relationship specifying hierarchical structure
of legal entities
In conversion A relationship specifying responsibility of a pers
on for another person.
Duality
In exchange A relationship between one or more economic
events linked to inflow, and one or more econ
omic events linked to outflow
In conversion A relationship between one or more economic
events linked to produce, and one or more econ
omic events linked to use or consume
Participation
In exchange A relationship between an economic event and
an agent receiving and losing rights to economic
resources, i.e. common concept for provide and
receive
In conversion A relationship between an economic event and
an agent receiving and losing physical control
over economic resources, i.e. common concept
for provide and receive
Stockflow
In exchange A relationship between an economic event and a
resource specifying inflow and outflow of the
rights to the resource
In conversion A relationship between an economic event and a
resource specifying produce, use and consume
of the resource
Commitment
In exchange Promise of an economic event representing inf
low or outflow of resources
In conversion Promise of an economic event representing prod
uce, use or consume of resources
Contract/Schedule
In exchange A collection of commitments and terms, which
are components of a contract
In conversion A collection of commitments and terms which
are components of a schedule


<!-- Page 353 -->


B. Notes on Modeling

This book contains many examples of business models. All of them have
something in common. We have tried to formulate the fundamental princip
les that the models in this book try to follow. We started to compile these
principles in order to capture the essence of the way we model business
processes and, consequently, design business applications. The ultimate
goal is to design a scalable business process model of the enterprise that is
open to extensions over time.

This section describes the driving force behind such models. The princip
les can be used to evaluate various business process modeling app
roaches, as well as your own models if you want them to have the same
essence as the models in this book.

B.1 There Is No Top-Level Business Process

We consider a business system as a value chain of independent processes.
The approach with the top-level process, which is decomposed into lowerl
evel processes, is suitable only for very simple systems. The top-level
processes often tend to change as the business application evolves. What
has been originally perceived as the top process might become less import
ant over time as the business conditions change. The top-down approach
is useful for describing existing systems, and for developing systems that
are static, but not as a method for developing systems that can evolve duri
ng time.

B.2 Premature Sequential Ordering Is Not Advisable
Many approaches to business modeling describe business processes as
scenarios or sequences of tasks: “First I receive a customer order. Next, I
fulfill the order. Then, I receive the payment from the customer.”

This approach has both advantages and disadvantages. Its advantage is
that the description it is easy to understand. It gives users a time axis, and


<!-- Page 354 -->


352 _B. Notes on Modeling

well-defined points on where to start and where to end. It is often useful to
offer such a view of business processes, simply because describing busin
ess processes in this way is intuitive for the users.

However, when we design a software solution that should support the a
process specified as a sequence of steps, we quickly realize that the goal of
the process can be reached in more than one specific sequence: “For some
orders, we require that customer pay before we fulfill the orders.” Moreo
ver, there are exceptions: “The customer might return the goods”; “The
customer might not pay for the goods in due time and must pay a penalty”;
“Sometimes, we cannot fulfill the order.“ The precise and complete des
cription of a business process in the form of a scenario, necessary for the
executable software model, then becomes overly complex.

A better approach is to focus on the essence of the business processes by
describing the purpose of the processes and the list of the applicable activit
ies, but defer as long as possible specifying their order of execution. If
there are constraints that restrict the order of the activities, they can be exp
ressed as logical constraints rather than temporal constraints. Often the
ordering can be postponed to as late as runtime, and business processes
emerge over time from specified logical constraints.

B.3 Bottom-Up Approach for Designing the System, and

Top-Down Approach for Explaining It Are Advisable
The top-down approach, also called functional decomposition, means starti
ng with the system as a single function, decomposing it into a small numb
er of subsystems, and repeating this process for each subsystem until your
reach the level granularity that allows implementation.

The top-down approach is useful in analysis and in developing an und
erstanding of the business system, but leads to monolithic design when
this approach is applied as a method for developing software.

In the bottom-up approach, we identify the atomic business processes,
create the components corresponding to the business processes as genera
lly as possible, and combine them into a business system. This approach
allows us to develop business components applicable in contexts other
than our specific system, and to adapt to changes over time.


<!-- Page 355 -->


B.4 Trading Partner View and Independent View 353
B.4 Trading Partner View and Independent View

The models in this book are created from the perspective of a company;
we call this company enterprise. If a model of the same phenomenon is
seen from the perspective of another company, we receive a “mirror ima
ge” of this model. For example, a purchase order for the enterprise is a
sales order for the enterprise’s customer; and the enterprise’s sales order is
a purchase order for its vendor.

Models in the trading partner view (i.e. the views of the Customer and
the Vendor) are illustrated in Fig. 227. For example, the Sales process of
the Vendor specifies an outflow of Goods and inflow of Cash to the Vend
or. The model of the same process from the perspective of the Customer
is a mirror image of the vendor’s Sales process in Fig. 227. The Purchase
process of the Customer specifies the inflow of Goods and outflow of
Cash from the Customer.

Customer's Model
«exchange process» Purchase
«resource» «agent» «receive» «agent» «resource»
Goods Vendor Customer Cash
id vay «provide»
inflow» «provide» | «receive» ROutTONe:
«increment» «decrement»
Goods «exchange» Cash
Receipt Disbursement
This = the |X This is the IX
same event same event
i Vendor's Model }
i «exchange process» Sales /
~—
Shipment Cash Receipt
«outflow» ; . «inflow»
«provide» «receive»—§€ «provide»
«receive»
«resource» «agent» «agent» «resource»
Goods Vendor Customer Cash
Fig. 227. Exchange process, trading partner view


<!-- Page 356 -->


354 _B. Notes on Modeling

In contrast to the trading partner models created from the perspective of
the enterprise, we can create the models from the perspective of an indep
endent observer. This independent view is useful in modeling the supply
chain collaboration. An independent view is illustrated in Fig. 228.

Note that in the independent view the concepts of increment and decrem
ent do not exists, economic events represent transfer. Likewise, inflow
and outflow do not exist, and are represented by stockflow.

Intdependent Observer's Model
«exchange process» Purchase/Sales
«agent»
Company A
; «provide»
«receive»
«transfer»
Goods Transfer «stockflow»
«exchange»
«stockflow»
Transfer
«receive»
«provide»
«agent»
Company B
Fig. 228. Exchange process, independent view

Models in this book are created using the trading partner view, as is il-

lustrated in Fig. 227.

B.5 Levels of Granularity

The level of granularity describes the size of the modeling elements. The
modeling elements at a high level of granularity can be decomposed to the
modeling elements at a lower level of granularity.

The entities at the highest level of granularity in this book are REA
business processes, such as sales process, procurement, warehouse mana
gement, and human resources. Each business process can be implemented


<!-- Page 357 -->


B.6 Models, Metamodels and UML 355
as an independent business application, but there are business applications
that cover several business processes.

Each REA business process can be decomposed into REA entities, such
as economic resources, events, agents, contracts, and claims.

The lowest level of granularity is the task level, describing details about
how to perform each economic event. We have not discussed the task
modeling in this book, as this is well known from techniques such as flowc
harts and workflows.

Value Chain Level

‘ Resource
«decomposition»
a Business Process ‘
| 1
Level of Decrement | {ality Increment
granularity Resource Event Event Resource
a a
«decomposition»
Y Task Level
Increment or Decrement Event
Signal
Fig. 229. Levels of granularity
B.6 Models, Metamodels and UML
We use UML (UML Superstructure Specification 2005) for the notation in
the models and diagrams in this book. When we refer in a text to a concept
shown in a diagram, we write it in italics.


<!-- Page 358 -->


356 __B. Notes on Modeling

The diagram in Fig. 230 illustrates models of the real world at three leve
ls of abstraction: the runtime model, the REA application model and the
REA metamodel.

REA Metamodel

1 1.4
Economic Resource Decrement
outflow
| |
«applyMetaData» «applyMetaData»

| |

I |

REA Application Model '
«economic resource» 1 0..* «decrement»

Cash «outflow» Cash Disbursement

ownership x

1 \ |

| \ |

1
«instanceOf» «instanceOf» «instanceOf»

! \

Runtime Model

! I
| !
I ! 1
' ownership
Cash Disbursement
|
|
«represents» «represents» «represents»
i l
|
Real World Vv WV Vv
“a SF a er Nw
= =
Fig. 230. Metamodel, application model, runtime model and the real world

The real world contains the actual real world entities that users of busin
ess applications want to monitor using the application.

The runtime model contains software representations of the real world
entities, for example, in a computer memory or in a database. We call the
entities in this model runtime instances. In UML, the names of the entities
in the runtime model are written with an underlined font, such as Cash.


<!-- Page 359 -->


B.6 Models, Metamodels and UML 357

The REA application model is an actual configuration of a business
software application. This model specifies the behavior and structure of the
instances in the runtime model. For example, the application model in
Fig. 230 specifies that every Cash instance must be related to zero or more
Cash Disbursement instances. The runtime model conforms to this rule:
there are two Cash objects, one is related to Cash Disbursement and the
other is not. Cash may even be related to several Cash Disbursements, for
example, if the same bill is received and given away several times. In the
UML, the names of entities in the application model are shown in bold
font, such as Cash. The text in guillemets, such as «outflow», represents
the names of metamodel elements shown in the application model. This
naming convention is not strictly UML (where text in guillemets means
stereotypes), but we use it for convenience.

The REA metamodel level specifies constraints and rules for constructi
ng application models. For example, the metamodel in Fig. 230 specifies
that every economic resource must be related to one or more decrements,
and the application model conforms to this rule: the Cash resource is rel
ated to the Cash Disbursement decrement. However, we can construct
other application models in which the Cash would be related to several
decrements, such as Cash Disbursement and Penalty Payment.


<!-- Page 360 -->


C. Patterns and Pattern Form

Patterns are elements of reusable design. Patterns specify abstractions and

models above the implementation level; thus, a pattern can be imple-

mented in many different ways depending on the technical and implemen-

tation platform. Patterns have usually carefully selected names, therefore,

patterns also create a common vocabulary for expressing designing con-

cepts. The patterns in this book are written in a modified Coplien form

(Coplien 1996); each pattern consists of the following sections:

e Name is a name of the pattern. References to a pattern are written in
capital italics, e.g., REA EXCHANGE PROCESS.

e Context describes the situation in which the pattern may be applied.

e Problem formulates a problem that repeatedly arises in the given cont
ext.

e Forces are constraints that restrict the solution of the problem, requirem
ents, and properties that the solution must have.

e Solution, in this book, is a model that solves the problem and satisfies
the forces.

e Design shows how the solution can be implemented in a software applic
ation.

e Examples illustrate how the pattern can be applied.

e Resulting Context outlines consequences of the solution that the user
should be aware of.

When reading a pattern, we recommend focusing on the Problem and
the Solution sections first. The problem and solution usually capture the
essence of the pattern, and other sections are needed to understand the det
ails.

However, to fully understand what patterns are all about, we recomm
end the readers to write one or two. Pattern writers can get expert help in
writing (and, consequently, understanding) patterns if they submit them to
one of the pattern conferences, such as PLoP (Pattern Languages of Prog
rams). More information on pattern conferences, and patterns in general,
can be found at http://www.hillside.net.


<!-- Page 361 -->


References

Appleton B (2000) Patterns and Software: Essential Concepts and Terminology,
http://www.cmcrossroads.com/bradapp/docs/patterns-intro.html

Arlow J, Neustadt I (2003) Enterprise Patterns and MDA: Building Better Softw
are with Archetype Patterns and UML, Addison-Wesley Professional

Arnold TRJ (1991) Introduction to Materials Management, 3% edition, Prentice-

Hall Inc.

Borch SE (2004) Typification in REA, First International REA Technology Works
hop, Copenhagen 2004

Carroll L (1996) The Complete Illustrated Lewis Carroll, Wordsworth Editions,
Ltd. Herfordshire

Coad P, Lefebvre E, DeLuca J (1999) Java Modeling in Color with UML, Enterp
rise Components and Process, Prentice Hall PTR, New York

Cockburn A (2000) Writing Effective Use Cases, Addison-Wesley Professional

Coplien J (1996): Software Patterns, SIGS Publications, New York,

Czarnecki K. Eisenecker UW (2000): Generative Programming - Methods, Tools,
and Applications, Addison-Wesley

David JS (1997) Three events that define an REA Methodology for Systems
Analysis, Design and Implementation. Proceedings of the Annual Meeting of
the American Accounting Association, 1997

Dunn CL, Cherrington OJ, Hollander AS (2004) Enterprise Information Systems:
A Pattern Based Approach, McGraw-Hill/Irwin, New York

Evans E (2003) Domain-Driven Design: Tackling Complexity in the Heart of
Software, Addison-Wesley Professional

Eriksson HE, Penker M (2000) Business Modeling with UML, John Wiley &
Sons, Inc.

Fowler M (1996) Analysis Patterns: Reusable Object Models, Addison-Wesley
Professional

Graham I (1998) Requirements Engineering and Rapid Development, Addison
Wesley

Geerts GL, McCarthy WE (1997) Using Object Templates from the REA Acc
ounting Model to Engineer Business Processes and Tasks. Paper presented at
European Accounting Congress, Graz, Austria.

Geerts GL, McCarthy WE (2000a) The Ontological Foundations of REA Enterp
rise Information Systems. Paper presented at the Annual Meeting of the
American Accounting Association, Philadelphia, PA.


<!-- Page 362 -->


362 References

Geerts GL, McCarthy WE (2000b) Augmented Intensional Reasoning in Knowle
dge-Based Accounting Systems. Journal of Information Systems, Volume
14, No. 2, 2000, pp. 127-150.

Geerts GL, McCarthy WE (2002) An Ontological Analysis of the Primitives of the
Extended REA Enterprise Information Architecture” at http://www.msu.edu/
user/mcecarth4/

Greenfield J, Short K (2004) Software Factories: Assembling Applications with
Patterns, Models, Frameworks, and Tools, Wiley and Sons

Gruber TR (1996) A Translation Approach to Portable Ontologies. Knowledge
Acquisition, 5(2):199-220

Forman IR, Danforth SH (1998) Putting Metaclasses to Work, A New Dimension
in Object-Oriented Programming, Addison-Wesley Longmann, Inc.

Haugen, B (2005) Resources and Rights, Discussion in REA Technology Mailing
List, http://groups.yahoo.com/group/REATechnology

Hay DC (1996) Data Model Patterns, Conventions of Thought Dorset House Publ
ishing, New York

Hay DC, Healy KA (2000) Business Rules, What Are They Really? The Business
Rules Group

Hessellund A, Balthazar S, Chohan A (2005) REA-VMI Model, A General
Framework for Vendor Managed Inventory (In Danish). MSc. Thesis, IT Univ
ersity Copenhagen

Henglein F et al (2006): Formal Specification of Commercial Contracts, Journal
on Software Tools for Technology Transfer

Hollander AS, Denna E, Cherrington OJ (1999) Accounting Information Technolo
gy and Business Solutions, Irwin/McGraw-Hill

IDEFO, Integration Definition for Function Modeling (1993) National Institute of
Standards and Technology, FIPS publication 183, http://www.idef.com/
idef0.html

Jaquet M (2003) Realistic - A REA Model without Perspectives (In Danish).
MSc. Thesis, IT University Copenhagen

Kiczales G et al (1996) Aspect-Oriented Programming, ECOOP 1996, Jyvaskyla,
Finland

Lampe JC (2002) Discussion of an Ontological Analysis of the Economic Primit
ives of the Extended-REA Enterprise Information Architecture. International
Journal of Accounting Information Systems, March 2002 pp.17-34.

Lieberherr K J (1997) Inventor’s Paradox, http://www.ccs.neu.edu/research/
demeter/adaptive-patterns/AOP/IP.html

Marshall C (2000) Enterprise Modeling with UML: Designing Successful Softw
are Through Business Patterns, Addison Wesley Longman, Inc.

McCarthy WE (1982) The REA Accounting Model: A Generalized Framework
for Accounting Systems in a Shared Data Environment. The Accounting Rev
iew (July 1982) pp. 554-78

Mellor SJ, Balcer MJ (2002) Executable UML, A Foundation for Model-Driven
Architecture, Addison-Wesley

Meyer B (1997) Object-Oriented Software Construction, second edition, Prentice
Hall, Inc.


<!-- Page 363 -->


References 363

MDA Guide Version 1.0.1 (2003) OMG document omg/03-06-01.

Peyton-Jones S, Eber JM (2003): How to write a financial contract. In Jeremy
Gibbons and Oege de Moor, editors, The Fun of Programming. Palgrave
Macmillan

Polya G (1982) How to Solve It: A New Aspect of Mathematical Method, Princet
on University Press

Porter M (1980) Competitive Strategy: Techniques for Analyzing Industries and
Competitors, Free Press, New York

Rising L, Manns ML (2004) Fearless Change: Patterns for Introducing New Ideas,
Addison-Wesley Professional

Rothbard MN (1978) For a New Liberty, Libertarian Manifesto, Collier Macmill
an Publishers, London

Samuelson PA, Nordhaus WD (1989) 13 edition, Economics, McGraw-Hill, Inc.

Sowa JF (1999) Knowledge Representation: Logical, Philosophical, and Computat
ional Foundations, Course Technology

Silverston L, Inmon WH, Graziano K (1997) Data Model Resource Book, A Lib
rary of Logical data Models and Data Warehouse Designs, John Wiley &
Sons, New York, Chichester, Weinheim, Brisbane, Singapore, Toronto

UML 2.0 Superstructure Specification (2005), OMG document formal/05-07-04


<!-- Page 364 -->


Index
account cash receipt, 261
financial, 189 cash sale, 260
in double entry, 191 category, 165
in REA, 184 chain of conversion processes, 278
inventory, 191 claim, 16, 30, 193
account level, 188 classification, 164
activity diagram, 79 age, 168
addition, 185 customer group, 168
address commitment, 3, 91
as location, 173 fully specified, 97
as notification, 216 violated, 333
e-mail, 162 consume relationship, 44, 69
internet, 162 contact person, 32
shipment, 174 context
advertising, 308 in pattern form, 357
agreement, 104 contract, 3
apartment, 19, 24 clauses, 100
application model, 355 examples, 321
archetype, 3 terms, 100
aspect-oriented programming, 150 control, 56
assembly, 282 conversion
author, 221 factor, 226
process, 3, 38, 47, 64
balance, 188 reciprocity, 93, 95
behavioral patterns, 147, 231 conversion process, 42
bill of material, 119 cost, 73
business process courier service, 291
alternative models, 78 credit memo, 196
conversion, 42 crosscutting, 150, 232
exchange, 13 currency class, 134
value chain, 62 custody, 123
business semantics, 231 customer, 6
cargo, 175 data access layer, 129, 136
cash, 269 Data Universal Numbering System,
cash account, 189 163


<!-- Page 365 -->


366 Index
database, 129, 135, 249 electricity, 317, 319
date employee, 275
in due date pattern, 205 employment, 327
in posting pattern, 178 enterprise, 2, 275, 322
of economic event, 23 entry
decrement event, 25 in double entry, 191
Description aspect, 239 in posting pattern, 179
description pattern, 211 European Article Numbering, 163
dimension events, 239
in account pattern, 188 exchange
in posting pattern, 179 duality, 26
domain model, 129 process, 3, 13, 15, 17, 21, 64
domain model component, 134 reciprocity, 93, 95
domain rules
at policy level, 97 features, 43, 57
for conversion process, 40 fees, 301
for exchange process, 15 financing, 269
for value chain, 64 flow chart, 79
double entry accounting, 191 forces, 14
duality in pattern form, 357
in conversion, 51 framework, 150
in exchange, 26 FulfillQ) method, 132
in ontology, 346 fulfillment, 92, 95
due date, 205 fulfillment page, 139
DueDate aspect, 236 function modeling method, 78
ebXML, 4 generic database model, 254
economic agent, 2 goods on stock, 191
in conversion process, 54 goods to receive, 191
in exchange process, 31 granularity, 352
economic event, 2 group, 82, 90
in conversion process, 46 guarantee, 328
in exchange process, 21
economic resource, 2, 70 ID field, 131
in conversion process, 42 IDEFO, 78
in exchange process, 17 Identification aspect, 236
in value chain, 69 identification aspect pattern, 154
individually identifiable, 74 identifier, 158
individually unidentifiable, 74 identifier setup, 158
transient, 317 increment, 25
economic resources independent view, 351
creating new, 274 inflow relationship, 19
individually identifiable, 266 initiator, 200
individually unidentifiable, 269 inspection, 282
modifying, 281 insurance, 331
education, 298 intellectual property, 55


<!-- Page 366 -->


Index 367
International Standard Book name
Number, 163 employee, 162
International Standard Music in pattern form, 357
Number, 163 Name field, 131
International Standard Serial note, 220, 221
Number, 163 notification, 215
inventor’s paradox, 229 Notification aspect, 238
invoice, 195
itinerary, 175 offer, 101
OLAP, 129
Joe’s Pizzeria, 5, 35 OLAP cube, 141
Joe’s Web, 129, 137 Open-edi, 4
order website, 127
labor, 77, 275, 327 outflow relationship, 19, 69
acquisition, 10, 293, 326
of the salesmen, 292 participation relationship, 346
lend, 22 pattern form, 357
level rule, 188 pattern map, 10
library, 17, 22, 34, 196 payment line, 101
linkage, 117 people management, 295
list price, 73 pleomorph, 3
loan PLoP conference, 357
of individually identifiable policy, 110
resources, 266 position, 173
of individually unidentifiable posting, 178, 185
resources, 269 price, 194, 200, 204, 227, 264, 302,
receipt, 269 324
return, 269 problem
location, 23, 172 in pattern form, 357
produce relationship, 44, 69
maintenance, 26, 56, 281 product
marketing, 308 creating new, 274
material issue, 275 intermediate, 278
materialized claim, 192 modifying, 281
matrix rule, 114 return, 263
message, 216 serial number, 162
metamodel, 355 product return, 330
Microsoft Navision, 242 project, 108
mitigation plan, 107 provide relationship
model level, 152 in conversion process, 55
model level, 151 in exchange process, 33
model-based framework, 243 purchase order, 322
modeling handbook, 257 purchase process, 9
moment in time, 23
money qualification, 168
bills and coins, 75 quantity, 23, 70, 227


<!-- Page 367 -->


368 Index
required, 118 storage, 249
used, 118 storing aspect data, 253
quantity on hand, 71 subtraction, 185
quote, 101 Sunday rule, 113
supervisor, 275
REA
ontology, 345 task, 233
value chain, 69 task management system, 233
what is, 2 TaskIdSequence, 234, 244
REA model, 129 tax group, 168
REA model component, 131 taxes, 301
receive relationship terminator, 200
in conversion process, 55 thirdness, 346
in exchange process, 33 time interval, 23, 48
reciprocity relationship, 93 time order of events
reconciliation, 199 in conversion process, 53
reconciliation method, 201 in exchange process, 29
rental, 24, 266 trading partner view, 7, 351
reservation relationship, 96, 340 transport, 175, 339
resource value flow, 64 type, 86, 90
responsibility, 120
rights unit of measure, 226
alternative models, 18 unit price, 73
in conversion process, 55 use relationship, 44, 69
in exchange process, 17 user interface, 240
route segment, 173
runtime model, 354 value, 28, 52, 224, 225
components of, 225
salary, 327 negative, 72
sale, 261 of the resource, 71
and shipment, 290 value chain, 59, 62
sales line, 101 process for creating, 65
sales order, 8, 101 value-added tax (VAT), 301
number, 161 vendor, 9
sales process, 6 Visual Basic, 235
sales tax, 301
schedule, 106 waste, 311
services as resources, 285, 314 weaving, 150
settlement, 194 web page, 127
social security number, 161 work breakdown structure, 119
solution
in pattern form, 357 XML document, 243
specification relationship, 87 XSL stylesheet, 245, 246, 250
stockflow relationship, 346


<!-- Page 368 -->

