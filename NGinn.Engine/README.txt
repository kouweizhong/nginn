DANE

1. Task
Ka¿dy task ma jakies parametry niezbedne do jego uruchomienia - np manual task ma osobe/grupe odpowiedzialna,
tytul, opis. Dodatkowo moze miec iles tam danych dodatkowych, ktore sa istotne dla logiki procesu (ale nie bezpoœrednio
dla zadania). Te dane sa w postaci 'input variables'.

Dodatkowo jest definiowany binding process data -> task input data
oraz drugi binding task output data -> process data

jak u nas bedzie wygladal binding? poprzez XSL.... a wiec mozna sobie zrobiæ i binding dla process input/otput data

<xsl:template>
	<tvariable><xsl:value-of select="/process_data/variable" /></tvariable>
	