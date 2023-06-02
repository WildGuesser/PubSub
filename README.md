# Sistemas Distribuidos

## P1 - Wholesaler

	0) Apagar ficheiros repositorio;  
	1) Implementação sockets cliente/servidor (para um so cliente);  
		1.1) Cliente escreve IP do server ao qual deve se connectar;  
		1.2) Responder com 200 - OK quando o cliente connectou-se ao server, 400 - BYE quando cliente digita "QUIT";  
		1.3) Ler ficheiro pelo servidor, tendo o caminho enviado pelo cliente (verificação da sua existência);
		1.4) Processamento do ficheiro; (POR FAZER)
	2) Multi-user:
		2.1) Uso de threads e mutex para enviar para o servidor o ficheiro CSV (com a estrutura: municipio,domicilio;  
															                             municipio,domicilio...)  
			2.2) - O servidor lê o ficheiro, envia de volta:  
					municipio,n°_domicilios (+, caso haja sobreposições)  
 

