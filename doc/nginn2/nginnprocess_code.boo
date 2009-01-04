struct_def "mojastrukt":
	pass
	
enum_def "mojenu", ["a", "b", "c", "d", "e"]


process "HandleRequest", 1:
	block "main":
		route "t1"
			split AND
			join AND
		
		timer "t2":
			split AND
			join AND
			data:
				variable {'name':'Lista', 'type':DateTime, 'dir':In, 'required':true}
				input_binding "Lista":
					expr data.Lista.Add("1")
				input_binding "Value", 3 + 3
				
					