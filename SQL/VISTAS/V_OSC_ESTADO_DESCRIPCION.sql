USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DE LA VISTA:		V_OSC_ESTADO_DESCRIPCION
	FECHA DE CREACIÓN: 		07/11/2025
	AUTOR:				RUBÉN
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\VISTAS
	USO:				##VISUAL##

	DESCRIPCION DE LA VISTA:	UNION DE TABLAS PARA MOSTRAR LA DESCRIPCIÓN DE ESTADOS DE CABECERAS DE SALIDA

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER VIEW V_OSC_ESTADO_DESCRIPCION
AS	
	SELECT
	OSC.PETICION, 
	OSC.NOMBRE_CLIENTE, 
	OSC.DIRECCION_ENTREGA,
	OSC.CODIGO_POSTAL,
	OSC.POBLACION,
	OSC.PROVINCIA,
	OSC.TELEFONO,
	OSC.F_CREACION,
	OSC.ESTADO,
	ES.DESCRIPCION 
FROM 
	ORDEN_SALIDA_CAB AS OSC
	
	INNER JOIN 
	ESTADO_SALIDA AS ES
	ON OSC.ESTADO = ES.ESTADO