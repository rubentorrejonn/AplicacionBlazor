USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DEL PROCEDIMIENTO:	PA_ASIGNAR_STOCK
	FECHA DE CREACIÓN: 		12/11/2025
	AUTOR:				Rubén
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\PROCEDIMIENTOS ALMACENADOS
	USO:				##VISUAL##

	FUNCIONAMIENTO:			Comprueba que hay stock de esa referencia en los palets y los deja en un estado "RESERVADO"

	PARAMETROS:			(OPCIONAL)
		PARAMETRO1 		INPUT	EXPLICACION
		PARAMETRO2 		OUTPUT	EXPLICACION

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER PROCEDURE PA_ASIGNAR_STOCK
	
	@PETICION	INT,

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
	
	DECLARE @REFERENCIA VARCHAR(30);
	DECLARE @CANTIDAD_REQUERIDA INT;
	DECLARE @CANTIDAD_ASIGNADA INT;
	DECLARE @CANTIDAD_DISPONIBLE INT;
	DECLARE @CANTIDAD_A_COGER INT;
	DECLARE @PALET INT;
	DECLARE @LINEA INT;

	--COMPROBACIONES
	IF @PETICION IS NULL
	BEGIN
		SET @MENSAJE = 'EL PARAMETRO ' + CAST(@PETICION AS VARCHAR) + ' ES REQUERIDO.'
		SET @RETCODE = 1
	END
	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		BEGIN TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
	----------------------------------------------------------------------------------------------------------------------------------------------

	--OPERACIONES
	--CURSOR PARA RECORRER LINEAS DE SALIDA
	DECLARE CUR_SALIDA CURSOR FOR
	SELECT 
		REFERENCIA, CANTIDAD, LINEA
	FROM 
		ORDEN_SALIDA_LIN
	WHERE 
		PETICION = @PETICION
	
	OPEN CUR_SALIDA;
	FETCH NEXT FROM CUR_SALIDA INTO @REFERENCIA, @CANTIDAD_REQUERIDA, @LINEA
	--MIENTRAS QUE SEA [ 0 = EXITOSA ]
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @CANTIDAD_ASIGNADA = 0;
		--CURSOR PARA RECORRER PALETS
		DECLARE CUR_PALETS CURSOR FOR
		SELECT
			PALET, CANTIDAD
		FROM 
			PALETS
		WHERE 
			REFERENCIA = @REFERENCIA AND ESTADO = '1'
		ORDER BY
			PALET
		OPEN CUR_PALETS;

		FETCH NEXT FROM CUR_PALETS INTO @PALET, @CANTIDAD_DISPONIBLE;
		WHILE @@FETCH_STATUS = 0 AND @CANTIDAD_ASIGNADA < @CANTIDAD_REQUERIDA
		BEGIN
			--SACAR CANTIDAD POR PALET
			IF(@CANTIDAD_REQUERIDA - @CANTIDAD_ASIGNADA) < @CANTIDAD_DISPONIBLE
			BEGIN
				SET @CANTIDAD_A_COGER = (@CANTIDAD_REQUERIDA - @CANTIDAD_ASIGNADA)
			END
			ELSE
			BEGIN
				SET @CANTIDAD_A_COGER = @CANTIDAD_ASIGNADA
			END
			--ASIGNAR EL PALET (CAMBIAR A ESTADO '3')
			UPDATE
				 PALETS
			SET
				 ESTADO = '3',
				 UBICACION = 'UBI-1'
			WHERE
				PALET = @PALET
			UPDATE
				ORDEN_SALIDA_CAB
			SET
				ESTADO = '2'
			WHERE
				PETICION = @PETICION

			--ASIGNAR VALORES A MOVIMIENTOS DENTRO DEL BUCLE
			INSERT INTO MOVIMIENTOS(
				PETICION,
				PALET, 
				CANTIDAD, 
				REFERENCIA, 
				UBICACION, 
				UBICACION_DESTINO, 
				LIN_PETICION, 
				REALIZADO
			)
			SELECT 
				@PETICION,
				@PALET,
				@CANTIDAD_A_COGER,
				@REFERENCIA,
				UBICACION = P.UBICACION,
				UBICACION_DEST = 'TRANSPORTE',
				@LINEA,
				REALIZADO = '1'
			FROM
				 PALETS AS P
			WHERE
				 P.PALET = @PALET


			-- ACTUALIZAR CANTIDAD ASIGNADA
			SET @CANTIDAD_ASIGNADA = @CANTIDAD_ASIGNADA + @CANTIDAD_DISPONIBLE
			FETCH NEXT FROM CUR_PALETS INTO @PALET, @CANTIDAD_DISPONIBLE;
		END

		CLOSE CUR_PALETS;
		DEALLOCATE CUR_PALETS;
		
		IF @CANTIDAD_ASIGNADA < @CANTIDAD_REQUERIDA
		BEGIN
			SET @MENSAJE = 'NO HAY SUFICIENTE STOCK DISPONIBLE PARA LA REFERENCIA ' + @REFERENCIA
			SET @RETCODE = 1
			RETURN @RETCODE
		END

		FETCH NEXT FROM CUR_SALIDA INTO @REFERENCIA, @CANTIDAD_REQUERIDA
	END
	CLOSE CUR_SALIDA;
	DEALLOCATE CUR_SALIDA;
		
	
	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		COMMIT TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
	----------------------------------------------------------------------------------------------------------------------------------------------


	SET @MENSAJE = 'El proceso se ha realizado correctamente.'
	SET @RETCODE = 0
	RETURN @RETCODE
END TRY
BEGIN CATCH
	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0 AND @@TRANCOUNT > 0				-- Si hay una transacción por encima no hacemos nada
	BEGIN
		ROLLBACK TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
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
