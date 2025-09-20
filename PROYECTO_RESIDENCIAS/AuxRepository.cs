using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data;
using static PROYECTO_RESIDENCIAS.Form1;

namespace PROYECTO_RESIDENCIAS
{
    public static class AuxRepository
    {
        // Guarda encabezado + detalle del pedido actual. Devuelve ID_PEDIDO Aux.
        public static int GuardarPedido(Pedido pedido, int mesaId, bool facturarAhora,
                                        string metodoPago, string formaPago, decimal importeRecibido,
                                        string auxCharset = "UTF8")
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: auxCharset);
            using var tx = aux.BeginTransaction();

            // Encabezado
            int idPedido;
            using (var cmd = new FbCommand(@"
INSERT INTO PEDIDOS (FECHA_HORA, MESA_ID, MESERO_ID, SUBTOTAL, IVA, TOTAL, FACTURAR, METODO_PAGO, FORMA_PAGO, IMPORTE_RECIBIDO, ESTADO)
VALUES (CURRENT_TIMESTAMP, @MESA, @MESERO, @SUB, @IVA, @TOT, @FAC, @MP, @FP, @REC, 'EN_CUENTA')
RETURNING ID_PEDIDO", aux, tx))
            {
                cmd.Parameters.Add(new FbParameter("@MESA", FbDbType.Integer) { Value = mesaId });
                cmd.Parameters.Add(new FbParameter("@MESERO", FbDbType.Integer) { Value = pedido.MeseroId });
                cmd.Parameters.Add(new FbParameter("@SUB", FbDbType.Decimal) { Value = pedido.Subtotal, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new FbParameter("@IVA", FbDbType.Decimal) { Value = pedido.Impuesto, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new FbParameter("@TOT", FbDbType.Decimal) { Value = pedido.Total, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new FbParameter("@FAC", FbDbType.SmallInt) { Value = facturarAhora ? 1 : 0 });
                cmd.Parameters.Add(new FbParameter("@MP", FbDbType.VarChar, 20) { Value = metodoPago ?? "" });
                cmd.Parameters.Add(new FbParameter("@FP", FbDbType.VarChar, 20) { Value = formaPago ?? "" });
                cmd.Parameters.Add(new FbParameter("@REC", FbDbType.Decimal) { Value = importeRecibido, Precision = 18, Scale = 2 });
                idPedido = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Detalle
            foreach (var d in pedido.Detalles)
            {
                using var cmd = new FbCommand(@"
INSERT INTO PEDIDO_DET (ID_PEDIDO, PARTIDA, CVE_ART, NOMBRE, CANTIDAD, PESO_GR, PRECIO_UNIT, IMPORTE, NOTAS)
VALUES (@ID, @PAR, @CVE, @NOM, @CANT, @GR, @PU, @IMP, @NOTA)", aux, tx);

                cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
                cmd.Parameters.Add(new FbParameter("@PAR", FbDbType.Integer) { Value = d.Partida });
                cmd.Parameters.Add(new FbParameter("@CVE", FbDbType.VarChar, 30) { Value = d.Clave ?? "" });
                cmd.Parameters.Add(new FbParameter("@NOM", FbDbType.VarChar, 200) { Value = d.Nombre ?? "" });
                cmd.Parameters.Add(new FbParameter("@CANT", FbDbType.Decimal) { Value = d.Cantidad, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@GR", FbDbType.Decimal) { Value = (object?)d.PesoGr ?? DBNull.Value, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@PU", FbDbType.Decimal) { Value = d.PrecioUnit, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@IMP", FbDbType.Decimal) { Value = d.Importe, Precision = 18, Scale = 2 });
                cmd.Parameters.Add(new FbParameter("@NOTA", FbDbType.VarChar, 255) { Value = d.Notas ?? "" });
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return idPedido;
        }

        public static void TransferirPedidoMesa(int idPedido, int mesaDestino)
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            using var cmd = new FbCommand("UPDATE PEDIDOS SET MESA_ID=@M WHERE ID_PEDIDO=@ID", aux);
            cmd.Parameters.Add(new FbParameter("@M", FbDbType.Integer) { Value = mesaDestino });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
            cmd.ExecuteNonQuery();
        }

        public static void CerrarPedido(int idPedido)
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            using var cmd = new FbCommand("UPDATE PEDIDOS SET ESTADO='CERRADA', FECHA_CIERRE=CURRENT_TIMESTAMP WHERE ID_PEDIDO=@ID", aux);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
            cmd.ExecuteNonQuery();
        }
    }
}
