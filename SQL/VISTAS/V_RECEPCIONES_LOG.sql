USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DE LA VISTA:		V_RECEPCIONES_LOG
	FECHA DE CREACIÓN: 		25/11/2025
	AUTOR:				RUBÉN
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\VISTAS
	USO:				##VISUAL##

	DESCRIPCION DE LA VISTA:	MOSTRAR UN VISOR DE LOG DE LAS RECEPCIONES

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

CREATE VIEW V_RECEPCIONES_LOG
AS	
	
	SELECT
		ALBARAN, 
		PROVEEDOR, 
		F_CREACION, 
		F_CONFIRMACION, 
		ESTADO, 
		DES_ESTADO
	FROM
		RECEPCIONES_CAB