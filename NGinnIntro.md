# Introduction #

  * What is 'NGinn'?
  * How can it be used?

# Details #

NGinn is a workflow engine for .Net capable of running business processes modelled in process modelling language, called also NGinn. NGinn process model is based on Petri nets and is very similar to YAWL process definition - actually NGinn is very much inspired by YAWL. NGinn language is xml-based so each process definition is actually an xml file. Xml describes the structure of process, which is a Petri net extended with data and business-process specific tasks.
Basically, a process definition is a set of interconnected activities (tasks), where connections between tasks define process 'flow' and express various dependencies between tasks  - for example 'task A can be executed after task B has completed' or 'task C cannot start until tasks A and B complete'.


NGinn Engine is a software package that hosts and runs NGinn processes, each business process instance starts and runs inside NGinn Engine. The engine provides process execution logic, interfaces to the outside world, GUI for users and persistence for process data. The engine can be run standalone (and communicate with other applications using .Net Remoting), or can be embedded inside an application.