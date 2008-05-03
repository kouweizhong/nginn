MUlti instance tasks - how to do data binding

Let's assume we want to create instance of task for each xml record (that is, for each element of an array variable) 
in the process data.

<xsl:for-each select="variableName">
	curNode = '.'
	calculateBindings for curNode -> this is task input data
	
now binding back
	we have a list of task output variable sets

<xsl:for-each select="task-result-xml">
	<outputVariableName>
		<task-out-binding-result />
	</outputVariableName>	
</xsl:for-each>

where can we bind it? to a single variable??? 
Well, it could be done. However, by default binding replaces the variable with new value.
In case of incomplete task output data, we would lose some information in the variable. It would be better to merge the variable
with task output data.

??? KA�DE zadanie mo�e sta� si� multi-instance, no bo co stoi na przeszkodzie ???

XML kontra .net variables

XML
+ przeno�ny format
+ �atwe definiowanie schematu
+ nie wymaga definiowania serializacji
+ �atwa walidacja

- trudno przetwarza�
- data binding jest skomplikowany
- utrudniony dost�p ze skryptu
- trudniejsze zarz�dzanie danymi w procesie

.net variables
+ �atwo pisa� wyra�enia
+ �atwo bindowa�
+ �atwiejsze operacje na danych
+ �adniejszy zapis

- brak walidacji
- brak definicji schematu






