TODO

co dalej
- zadania... niech one b�d� troch� bardziej autonomiczne

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

czy task moze si� sam 'cancelowa�'? na razie nie
sk�d task ma wiedzie� czy si� zako�czy�
ma dosta� event. Event zostanie dostarczony przez PI
