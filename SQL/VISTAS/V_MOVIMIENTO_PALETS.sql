USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DE LA VISTA:		V_MOVIMIENTO_PALETS
	FECHA DE CREACIÓN: 		14/11/2025
	AUTOR:				RUBÉN
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\VISTAS
	USO:				##VISUAL##

	DESCRIPCION DE LA VISTA:	MOSTRAS VISTA DE LA TABLA MOVIMIENTOS Y REFERENCIA PARA PODER MOVER PALETS RESERVADOS CON ESTADO 1

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER VIEW V_MOVIMIENTO_PALETS
AS	
	SELECT
	P.PALET, 
	R.REFERENCIA,
	R.DES_REFERENCIA,
	P.CANTIDAD,
	P.UBICACION

FROM 
	PALETS AS P

INNER JOIN 
	
	REFERENCIAS AS R

ON 
	P.REFERENCIA = R.REFERENCIA

INNER JOIN 
	
	UBICACIONES AS U

ON
	P.UBICACION = U.UBICACION

WHERE P.ESTADO = 1 AND U.STATUS_UBI = 0
