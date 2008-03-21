kickanie ¿ywego tokena
we: token w statusie 'ready'

1.	dla kazdego wychodzacego taska
		sprawdz czy moze odpalic
			jesli tak, to odpal
	jesli zaden task sie nie odpalil-przestaw token w 'czekanie na sasiadow'

wy: token w statusie
a) waiting jesli wystawiono choc jeden task
b) error jesli task dalby sie wystawic ale cos sie schrzanilo
c) waiting2 jesli nie mogl sie kicknac bo przejscie nie odpalilo

Do procesowania jednego tokena potrzebujemy znac stan wszystkich tokenow w procesie,
a przynajmniej tych ktore sa na wejsciu tranzycji wychodzacych z miejsca w ktorym jest token
