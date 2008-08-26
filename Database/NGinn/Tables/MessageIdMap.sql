create table MessageIdMap 
(id int primary key,
 message_id varchar(50) not null,
 task_id varchar(30) not null)
 go
 create index IDX_MessageIdMap_message_id on MessageIdMap(message_id)
 go
 