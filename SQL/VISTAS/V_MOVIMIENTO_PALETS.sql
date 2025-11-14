USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DE LA VISTA:		V_MOVIMIENTO_PALETS
	FECHA DE CREACIÓN: 		14/11/2025
	AUTOR:				RUBÉN
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\VISTAS
	USO:				##VISUAL##

	DESCRIPCION DE LA VISTA:	MOSTRAS VISTA DE LA TABLA MOVIMIENTOS Y REFERENCIA PARA PODER MOVER PALETS RESERVADOS CON ESTADO 3

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

CREATE VIEW V_MOVIMIENTO_PALETS
AS	
	SELECT
	M.PALET, 
	M.REFERENCIA,
	R.DES_REFERENCIA,
	M.CANTIDAD,
	M.UBICACION

FROM 
	MOVIMIENTOS AS M

INNER JOIN 
	
	REFERENCIAS AS R

ON 
	M.REFERENCIA = R.REFERENCIA

	
INNER JOIN 

	PALETS AS P

ON
	
	M.PALET = P.PALET

WHERE P.ESTADO = 3