USE [FCT_OCT_1]
GO
/****** Object:  StoredProcedure [dbo].[PA_RECEPCIONES_Y_DBMAIL]    Script Date: 07/11/2025 9:16:14 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DEL PROCEDIMIENTO:	PA_GUARDAR_PEDIDO
	FECHA DE CREACIÓN: 		07/11/2025
	AUTOR:				Rubén
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\PROCEDIMIENTOS ALMACENADOS
	USO:				##VISUAL##

	FUNCIONAMIENTO:			GUARDA LOS PEDIDOS SIN RESTAR EL STOCK Y ACTUALIZAR ESTADO A 1 DE CABECERA.

	PARAMETROS:			(OPCIONAL)
		PARAMETRO1 		INPUT	EXPLICACION
		PARAMETRO2 		OUTPUT	EXPLICACION

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER PROCEDURE [dbo].[PA_GUARDAR_PEDIDO]
	
	@PETICION	INT,
	@REFERENCIA	VARCHAR(30),
	@CANTIDAD	INT,
	

	@INVOKER	INT,		-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@USUARIO	VARCHAR(12),	-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@CULTURA	VARCHAR(5),

	@RETCODE	INT OUTPUT, --DEFINICIÓN OBLIGATORIA
	@MENSAJE	VARCHAR(1000)	OUTPUT	--DEFINICIÓN OBLIGATORIA

AS

BEGIN TRY
	--DECLARACION DE VARIABLES 

	DECLARE @N_TRANS		INT = 0	 --NUMERO DE TRANSACCIONES ACTIVAS	(@@TRANCOUNT)
	SET @N_TRANS = @@TRANCOUNT

	--COMPROBACIONES
	IF NOT EXISTS(SELECT * FROM ORDEN_SALIDA_LIN WHERE PETICION = @PETICION)
	BEGIN
		SET @MENSAJE = 'No existe la petición ' + CAST(@PETICION AS VARCHAR)
		SET @RETCODE = 1
	END
	
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		BEGIN TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------

	--OPERACIONES
	INSERT INTO ORDEN_SALIDA_LIN(PETICION, LINEA, REFERENCIA, CANTIDAD)
	SELECT @PETICION, ISNULL(MAX(LINEA), 0)  + 1, @REFERENCIA, @CANTIDAD
	FROM ORDEN_SALIDA_LIN
	WHERE PETICION = @PETICION


	--ACTUALIZAR ESTADO CABECERA
	UPDATE ORDEN_SALIDA_CAB
	SET ESTADO = 1
	WHERE PETICION = @PETICION;


	

	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		COMMIT TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------


	SET @MENSAJE = 'El proceso se ha realizado correctamente.'
	SET @RETCODE = 0
	RETURN @RETCODE
END TRY
BEGIN CATCH
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0 AND @@TRANCOUNT > 0				-- Si hay una transacción por encima no hacemos nada
	BEGIN
		ROLLBACK TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @MENSAJE = '' 
	BEGIN
		SET  @MENSAJE = ERROR_MESSAGE()
	END
	
	SET @RETCODE = -1
		
	RETURN @RETCODE
END CATCH

	SET @RETCODE = -1		
	RETURN @RETCODE






/*----------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------               PRUEBAS              ------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------


DECLARE @RETCODE	INT
DECLARE @PARAMETRO1	VARCHAR(10)
DECLARE @PARAMETRO2	INT
DECLARE @PARAMETRO3	VARCHAR(20)
DECLARE @MENSAJE	VARCHAR(1000)

SET @MENSAJE 		= ''

EXEC @RETCODE = PA_XXXX @PARAMETRO1, @PARAMETRO2, @PARAMETRO3 OUTPUT, @MENSAJE OUTPUT

PRINT 'RETCODE:	' 	+ ISNULL(CAST(@RETCODE AS VARCHAR(10)), 'NULO')
PRINT 'MENSAJE:	' 	+ ISNULL(@MENSAJE, 'NULO')
PRINT 'PARAMETRO3''	+ ISNULL(@PARAMETRO3, 'NULO')



------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------             FIN PRUEBAS            ------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------*/
