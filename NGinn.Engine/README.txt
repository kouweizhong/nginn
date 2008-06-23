TODO

co dalej
- zadania... niech one bêd¹ trochê bardziej autonomiczne

ProcessInstance
- enableTask
- cancelTask
Task callback
- taskEnabled
- taskCompleted
- taskCancelled

PI
--> enableTask
<-- taskStarted (opcjonalne) 
<-- taskCompleted (wymagane)
.

--> cancelTask
.

<-- taskCancelled (na razie nie implementujemy)

czy task moze siê sam 'cancelowaæ'? na razie nie
sk¹d task ma wiedzieæ czy siê zakoñczy³
ma dostaæ event. Event zostanie dostarczony przez PI
