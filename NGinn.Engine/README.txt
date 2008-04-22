DANE

kazdy task ma jakies dane we i wy. binding tych danych powinien byc standardem.
namespace... zalozmy ze dane w procesie maja jakis namespace
np zawsze z aliasem data/psvar/var1 
skad bierzemy dane we: wedlug deklaracji w procesie (process variables)
czy sa jakies inne zmienne?
lepiej dac tylko tyle ile w deklaracji.
czyli wg deklaracji mozemy importowac zmienne package/environment

czy to dla procesu, czy dla tasku, zawsze powinno dac sie wyznaczyc schemat danych (xml schema) 
no dobra
1. deklaracje zmiennych procesu/tasku...
2. namespace procesu/tasku hm po co nam to potrzebne? tylko do przetwarzania input xml
3. budujemy xml ze zmiennymi procesu = input + process + output + env + package variables
4. co z danymi ktore sa wewnetrzne dla procesu? Nie powinny nas chyba obchodzic, niech sobie w xml wstawia
   co mu sie zywnie podoba. wazne zeby na poczatku i na koncu byl poprawny xml, zgodny z definicja zmiennych procesu
h   
