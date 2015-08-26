## Important: NGinn source code is hosted at codeplex ##
## http://nginn.codeplex.com/ ##

# NGinn - Petri-net based workflow engine for .Net #
NGinn development blog - http://nginn.org/blog

NGinn's goal is to provide an easy to understand but expressive business process modeling language together with workflow engine capable of executing the processes. Main components of NGinn are (will be):

  * business process description language (called NGinn)
  * workflow engine for .Net capable of running NGinn processes
  * graphical process designer

NGinn is inspired by the YAWL projest (http://www.yawl-system.com) - a workflow language based on Petri nets, created by researchers at Queensland University of Technology and Eindhoven University of Technology. NGinn will provide similar language, but main focus of the project is to provide reliable process execution engine and to enable easy integration of NGinn engine with other applications.

**Status information**
Currently NGinn engine is in early development phase, only basic 'proof of concept' functionalities have been implemented. It is a bit too early to give first release date, but I think it will be known in next 2 months (that is, in june 08).
RG

---

Update (17.04.2008)
Engine will undergo some changes, some features are going to be introduced:
- package system will be introduced. Package will contain a number of process definitions sharing the same data schemas, each process will belong to some package.
- xml will be used for carrying process data, I'm working on variable definition/binding in the language

---

Current status (08.04.2008):
Engine is in development, currently it's not ready for use. Work is focused on implementing the process hosting engine and on finishing the language spec.
- basic engine functionality working
- first language schema ready
- no process designer yet, no documentation, no implementation of external interfaces, no worklist GUI, parts of nginn process definition language are missing, many runtime components are not implemented yet.
- but the project is doomed to success

---

Technologies used so far in the creation of Nginn:
  * Microsoft .Net 2.0
  * Sooda (http://sooda.sourceforge.net) - O/R mapping
  * NLog (http://nlog.sourceforge.net) - logging
  * Spring.Net framework (http://www.springframework.net) - component wireup
  * [Script.net](http://www.protsyk.com/scriptdotnet) - embedded scripting environment
  * Microsoft SQL server as the database backend (but nothing database specific will be introduced, so it can be changed to any other db)


---

NGinn blog - http://nginn.org/blog
NGinn-messagebus project - http://code.google.com/p/nginn-messagebus
[cnj](http://www.cnj.pl)