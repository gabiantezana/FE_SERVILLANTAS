using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace FE_SERVILLANTAS
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        private Timer tiempo = new Timer(Convert.ToInt32(ConfigurationManager.AppSettings["TimeService"].ToString())); //1 minutos

        protected override void OnStart(string[] args)
        {
            tiempo.Enabled = true;
        }

        protected override void OnStop()
        {
        }
        private string getCadenaConexion() => ConfigurationManager.ConnectionStrings["CadenaConexionBD"].ToString();












        private void Elapsed_Event(object sender, ElapsedEventArgs e)
        {

            //Constantes
            string str_RUC_EMPRESA = "20100292875";
            int int_NROCOMP = 20;
            string str_TipoCodigo = "1";
            string U_BPP_MDTD;
            decimal TotalPrepago = 0;
            DataTable dttListAll = new DataTable();// lista todos los documentos antes de la Firma Digital
            DataTable dttListAllEnvioFD = new DataTable();//lista todos los documentos que ya deben tener Firma Digital, preparados paraa pintar en la OINV Y ORIN
            DataTable dttListAllEnvioCDR = new DataTable();//lista todos los documentos que pueden consultar su CDR
            StreamWriter file;
            StreamReader fileReader;
            string rutanombre = "";
            string rutanombreEnvio = "";
            string contenido = "";
            string contenidoFirmaDigtal = "";
            DataTable dttContenido = new DataTable();
            DataTable dttContenidoInvoice = new DataTable();
            DataTable dttContenidoCreditNotes = new DataTable();
            DataTable dttContenidoDebitNotes = new DataTable();
            DataTable dttContenido1 = new DataTable();
            DataTable dttContenido2 = new DataTable();
            int docEntry = 0;
            string objtype = "";
            string docsubtype = "";
            string tipoDoc = "";
            string serie = "";
            string correlativo = "";
            DataTable dttContenidoNC = new DataTable();
            string rutanombreEnvioxml = "";
            string rutanombreEnvioxmlinter = "";
            string rutanombreEnviozip = "";
            string rutanombrezip = "";
            string RutaDestino = ConfigurationManager.AppSettings["Ruta"];
            string WsEfactUserV = ConfigurationManager.AppSettings["WsEfactUser"];
            string WsEfactPasswordV = ConfigurationManager.AppSettings["WsEfactPassword"];
            int documentTypeWS = 0;//   Atributo o tipo de documento que se enviara para consultar el CDR
            string IdentifierWS = "";//   F001-00000001, del documento a consultar el CDR


            SqlConnection con = new SqlConnection(getCadenaConexion());
            SqlConnection con1 = new SqlConnection(getCadenaConexion());
            SqlConnection con2 = new SqlConnection(getCadenaConexion());
            SqlConnection conUpdateCreate = new SqlConnection(getCadenaConexion());

            try
            {//Lista los documentos aptos que se envia para ser Firmados

                var sqldatalistar = new SqlDataAdapter("[dbo].[MSS_FE_LISTA_TODOS_DOC]", con);
                con.Open();
                sqldatalistar.SelectCommand.CommandType = CommandType.StoredProcedure;
                sqldatalistar.Fill(dttListAll);
                con.Close();

                docEntry = Convert.ToInt32(dttListAll.Rows[0]["DocEntry"]);
                objtype = Convert.ToString(dttListAll.Rows[0]["ObjType"]);
                docsubtype = Convert.ToString(dttListAll.Rows[0]["DocSubType"]);
                tipoDoc = Convert.ToString(dttListAll.Rows[0]["TipoDoc"]);
                serie = Convert.ToString(dttListAll.Rows[0]["Serie"]);
                correlativo = Convert.ToString(dttListAll.Rows[0]["Correlativo"]);


                if (objtype == "13")
                {
                    try
                    { //Trabaja el primer documento
                        var sqldata01 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF]", con);//SEI_FEX_DebitNotes
                        con.Open();

                        sqldata01.SelectCommand.CommandType = CommandType.StoredProcedure;
                        sqldata01.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724;
                        sqldata01.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                        sqldata01.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                        sqldata01.Fill(dttContenido);
                        con.Close();

                        foreach (DataRow row in dttContenido.Rows)
                        {

                            if (Convert.ToString(row["Line"]) == "FF00FF")
                                contenido = contenido + Convert.ToString(row["Line"]);

                            else
                                contenido = contenido + Convert.ToString(row["Line"]) + Environment.NewLine;

                        }



                        if (tipoDoc == "01")
                            rutanombre = RutaDestino + "daemon\\documents\\in\\invoice\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".csv";


                        if (tipoDoc == "03")
                            rutanombre = RutaDestino + "daemon\\documents\\in\\boleta\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".csv";



                        if (tipoDoc == "08")
                            rutanombre = RutaDestino + "daemon\\documents\\in\\debitnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".csv";

                        file = new System.IO.StreamWriter(rutanombre, true, UTF8Encoding.Default);// Creacion de .csv without bom

                        file.WriteLine(contenido);
                        file.Close();

                        try
                        { //Se creo documento en la ruta definida
                            var sqldataUpdateCreate = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_CREATEFILE]", conUpdateCreate);// [dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_CREATEFILE]
                            conUpdateCreate.Open();

                            sqldataUpdateCreate.SelectCommand.CommandType = CommandType.StoredProcedure;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;
                            sqldataUpdateCreate.Fill(dttContenido);
                            conUpdateCreate.Close();
                        }
                        catch (Exception ex)
                        {
                            conUpdateCreate.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        con.Close();
                    }
                }

                if (objtype == "14")
                {
                    try
                    {
                        //Registrar Factura, Boleta, Nota de Crédito, Nota de Débito
                        var sqldata02 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF]", con);
                        con.Open();

                        sqldata02.SelectCommand.CommandType = CommandType.StoredProcedure;
                        sqldata02.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                        sqldata02.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                        sqldata02.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724

                        sqldata02.Fill(dttContenidoNC);
                        con.Close();

                        foreach (DataRow row in dttContenidoNC.Rows)
                        {

                            if (Convert.ToString(row["Line"]) == "FF00FF")

                                contenido = contenido + Convert.ToString(row["Line"]);// Evitar 2 salto de linea
                            else
                                contenido = contenido + Convert.ToString(row["Line"]) + Environment.NewLine;
                        }

                        rutanombre = RutaDestino + "daemon\\documents\\in\\creditnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".csv";

                        file = new System.IO.StreamWriter(rutanombre, true, UTF8Encoding.Default);

                        file.WriteLine(contenido);
                        file.Close();
                        con.Close();

                        try
                        {
                            //Se creo documento en la ruta definida
                            var sqldataUpdateCreate = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_CREATEFILE]", conUpdateCreate);
                            conUpdateCreate.Open();

                            sqldataUpdateCreate.SelectCommand.CommandType = CommandType.StoredProcedure;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;
                            sqldataUpdateCreate.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;
                            sqldataUpdateCreate.Fill(dttContenido);
                            conUpdateCreate.Close();
                        }
                        catch (Exception ex)
                        {
                            conUpdateCreate.Close();
                        }
                    }
                    catch (Exception ex)
                    {

                        con.Close();
                        //System.Windows.Forms.MessageBox("asdasd")

                    }
                }

            }
            catch (Exception ex) { }



            //-------------------------------------------------------------------------------------------------

            //Para registrar en web service.
            try
            {

                var sqldatalistarEnvioFD = new SqlDataAdapter("[dbo].[MSS_FE_LISTA_TODOS_DOC_ENVIOWS]", con1);
                con1.Open();
                sqldatalistarEnvioFD.SelectCommand.CommandType = CommandType.StoredProcedure;
                sqldatalistarEnvioFD.Fill(dttListAllEnvioFD);
                con1.Close();

                docEntry = Convert.ToInt32(dttListAllEnvioFD.Rows[0]["DocEntry"]);//
                objtype = Convert.ToString(dttListAllEnvioFD.Rows[0]["ObjType"]);
                docsubtype = Convert.ToString(dttListAllEnvioFD.Rows[0]["DocSubType"]);
                tipoDoc = Convert.ToString(dttListAllEnvioFD.Rows[0]["TipoDoc"]);
                serie = Convert.ToString(dttListAllEnvioFD.Rows[0]["Serie"]);
                correlativo = Convert.ToString(dttListAllEnvioFD.Rows[0]["Correlativo"]);



                if (objtype == "13")
                {
                    try
                    {
                        if (tipoDoc == "01")
                        {
                            //var  oneLine As String
                            try
                            {
                                //Inicio_LecturadeFirmaDigital
                                rutanombreEnvio = RutaDestino + "daemon\\documents\\out\\invoice\\" + str_RUC_EMPRESA + " - " + tipoDoc + "-" + serie + "-" + correlativo + ".txt";
                                fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                contenidoFirmaDigtal = fileReader.ReadLine();
                                fileReader.Close();
                                //Fin_LecturadeFirmaDigital

                                //Actualizacion de FirmaDigital en campo respectivo
                                var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_FD]", con1);
                                con1.Open();
                                sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal;//724
                                sqldata03.Fill(dttContenido1);
                                con1.Close();

                                //Libre para generar 
                                try
                                {

                                    sqldata03.Fill(dttContenido1);
                                    con1.Close();

                                    rutanombreEnviozip = RutaDestino + "daemon\\documents\\out\\invoice\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".zip";

                                    fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                    contenidoFirmaDigtal = fileReader.ReadLine();
                                    fileReader.Close();

                                    var contenidoenbase64 = new Byte[] { };
                                    contenidoenbase64 = ConvertFileToBase64(rutanombreEnviozip);


                                    //Inicio_autorizacion
                                    var credenciales = new WSServillantas.authorization();
                                    credenciales.user = WsEfactUserV;
                                    credenciales.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var client = new WSServillantas.TransactionServiceClient();
                                    var response = new WSServillantas.transactionResponse[] { };// Para la respuesta

                                    response = client.sendInvoice(credenciales, contenidoenbase64);//contenidoenbase64()

                                    //client.sendInvoice(transcionreporte)

                                    //[ERP] Unable to read JAR file for client
                                    var outstringresponde = "";
                                    outstringresponde = response[0].outString;

                                    //if (outstringresponde = "[ERP] Success") {  ;// Si paso exitosamente el documento
                                    if (outstringresponde == "[ERP] Success." || outstringresponde == "[ERP] Success" || outstringresponde == "[ERP] The UUID into document already exists in the system" || outstringresponde == "[ERP] The submitted document already exists in the system")
                                    {

                                        //Actualizacion de FirmaDigital en campo respectivo
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_REGISTRAR_WS]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                                                                                                                    //sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();
                                    }
                                    //Libre para generar 

                                    else
                                    {
                                        //Cuando no a pasado de manera exitosa
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_OBSENVIO]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                        ;//sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();



                                    }
                                }

                                catch (Exception ex)
                                {
                                    con1.Close();
                                }

                            }
                            catch (Exception ex) { }
                        }

                        if (tipoDoc == "03")
                        {

                            try
                            {

                                rutanombreEnvio = RutaDestino + "daemon\\documents\\out\\boleta\\" + str_RUC_EMPRESA + " - " + tipoDoc + "-" + serie + "-" + correlativo + ".txt";
                                fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                contenidoFirmaDigtal = fileReader.ReadLine();
                                fileReader.Close();

                                //--
                                var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_FD]", con1);
                                con1.Open();

                                sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal;//724
                                sqldata03.Fill(dttContenido1);
                                con1.Close();

                                //Libre para generar 
                                try
                                {
                                    rutanombreEnviozip = RutaDestino + "daemon\\documents\\out\\boleta\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".zip";

                                    fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                    contenidoFirmaDigtal = fileReader.ReadLine();
                                    fileReader.Close();

                                    var contenidoenbase64 = new Byte[] { };
                                    contenidoenbase64 = ConvertFileToBase64(rutanombreEnviozip);

                                    //Inicio_autorizacion
                                    var credenciales = new WSServillantas.authorization();
                                    credenciales.user = WsEfactUserV;
                                    credenciales.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var clientBV = new WSServillantas.TransactionServiceClient();
                                    var response = new WSServillantas.transactionResponse[] { };// Para la respuesta

                                    response = clientBV.sendBoleta(credenciales, contenidoenbase64);//contenidoenbase64()

                                    //[ERP] Unable to read JAR file for client
                                    var outstringresponde = "";
                                    outstringresponde = response[0].outString;

                                    if (outstringresponde == "[ERP] Success." || outstringresponde == "[ERP] Success" || outstringresponde == "[ERP] The UUID into document already exists in the system" || outstringresponde == "[ERP] The submitted document already exists in the system")
                                    {

                                        //if (outstringresponde = "[ERP] Success") {  ;// Si paso exitosamente el documento 

                                        //Actualizacion de FirmaDigital en campo respectivo
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_REGISTRAR_WS]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                                                                                                                    //sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();
                                        //Libre para generar 
                                    }
                                    else
                                    {//Cuando no a pasado de manera exitosa
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_OBSENVIO]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                                                                                                                    //sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();
                                    }

                                }


                                //var  prueba As String = "adasd"

                                catch (Exception ex)
                                {
                                    con1.Close();
                                }

                            }
                            catch (Exception ex) { }
                        }
                        //DN()

                        if (tipoDoc == "08")
                        {

                            try
                            {

                                rutanombreEnvio = RutaDestino + "daemon\\documents\\out\\debitnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + "-" + serie + "-" + correlativo + ".txt";
                                fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                contenidoFirmaDigtal = fileReader.ReadLine();
                                fileReader.Close();

                                //--
                                var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_FD]", con1);
                                con1.Open();

                                sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                sqldata03.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal;//724
                                sqldata03.Fill(dttContenido1);
                                con1.Close();

                                //Libre para generar 
                                try
                                {

                                    rutanombreEnviozip = RutaDestino + "daemon\\documents\\out\\debitnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".zip";

                                    fileReader = System.IO.File.OpenText(rutanombreEnvio);
                                    contenidoFirmaDigtal = fileReader.ReadLine();
                                    fileReader.Close();

                                    var contenidoenbase64 = new Byte[] { };
                                    contenidoenbase64 = ConvertFileToBase64(rutanombreEnviozip);

                                    //Inicio_autorizacion
                                    var credenciales = new WSServillantas.authorization();
                                    credenciales.user = WsEfactUserV;
                                    credenciales.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var clientND = new WSServillantas.TransactionServiceClient();
                                    var response = new WSServillantas.transactionResponse[] { };// Para la respuesta

                                    response = clientND.sendDebitNote(credenciales, contenidoenbase64);//contenidoenbase64()

                                    //[ERP] Unable to read JAR file for client
                                    var outstringresponde = "";
                                    outstringresponde = response[0].outString;

                                    if (outstringresponde == "[ERP] Success." || outstringresponde == "[ERP] Success" || outstringresponde == "[ERP] The UUID into document already exists in the system" || outstringresponde == "[ERP] The submitted document already exists in the system")
                                    {

                                        //if (outstringresponde = "[ERP] Success") {  ;// Si paso exitosamente el documento 

                                        //Actualizacion de FirmaDigital en campo respectivo
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_REGISTRAR_WS]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                                                                                                                    //sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();
                                    }
                                    //Libre para generar 

                                    else
                                    {
                                        //Cuando no a pasado de manera exitosa
                                        var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_OBSENVIO]", con1);
                                        con1.Open();
                                        sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                        sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                        sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                        sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                                                                                                                    //sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                        sqldata04.Fill(dttContenido1);
                                        con1.Close();
                                    }


                                }

                                //var  prueba As String = "adasd"

                                catch (Exception ex)
                                {
                                    con1.Close();
                                }
                            }

                            catch (Exception ex)
                            {

                                con.Close();
                                //System.Windows.Forms.MessageBox("asdasd")
                            }
                        }
                    }
                    catch (Exception ex) { }
                }

                if (objtype == "14")
                {

                    try
                    {

                        rutanombreEnvio = RutaDestino + "daemon\\documents\\out\\creditnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".txt";
                        fileReader = System.IO.File.OpenText(rutanombreEnvio);
                        contenidoFirmaDigtal = fileReader.ReadLine();
                        fileReader.Close();


                        var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_FD]", con1);
                        con1.Open();


                        sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                        sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                        sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                        sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                        sqldata03.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal;//724

                        sqldata03.Fill(dttContenido1);
                        con1.Close();


                        try
                        {
                            sqldata03.Fill(dttContenido1);
                            con1.Close();

                            rutanombreEnviozip = RutaDestino + "daemon\\documents\\out\\creditnote\\" + str_RUC_EMPRESA + " - " + tipoDoc + " - " + serie + " - " + correlativo + ".zip";

                            fileReader = System.IO.File.OpenText(rutanombreEnvio);
                            contenidoFirmaDigtal = fileReader.ReadLine();
                            fileReader.Close();

                            var contenidoenbase64 = new Byte[] { };
                            contenidoenbase64 = ConvertFileToBase64(rutanombreEnviozip);

                            //Inicio_autorizacion
                            var credenciales = new WSServillantas.authorization();
                            credenciales.user = WsEfactUserV;
                            credenciales.password = WsEfactPasswordV;
                            //Fin_autorizacion

                            var client = new WSServillantas.TransactionServiceClient();
                            var response = new WSServillantas.transactionResponse[] { };// Para la respuesta

                            response = client.sendCreditNote(credenciales, contenidoenbase64);//contenidoenbase64()

                            //[ERP] Unable to read JAR file for client
                            var outstringresponde = "";
                            outstringresponde = response[0].outString;

                            if (outstringresponde == "[ERP] Success." || outstringresponde == "[ERP] Success" || outstringresponde == "[ERP] The UUID into document already exists in the system" || outstringresponde == "[ERP] The submitted document already exists in the system")
                            {

                                //if (outstringresponde = "[ERP] Success") {  ;// Si paso exitosamente el documento 

                                //Actualizacion de FirmaDigital en campo respectivo
                                var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_REGISTRAR_WS]", con1);
                                con1.Open();
                                sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                ;//sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                sqldata04.Fill(dttContenido1);
                                con1.Close();
                                //Libre para generar 
                            }
                            else
                            {
                                //Cuando no a pasado de manera exitosa
                                var sqldata04 = new SqlDataAdapter("[dbo].[MSS_FE_PRINCIPAL_DF_UPDATE_OBSENVIO]", con1);
                                con1.Open();
                                sqldata04.SelectCommand.CommandType = CommandType.StoredProcedure;
                                sqldata04.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                sqldata04.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                sqldata04.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                ;//sqldata04.SelectCommand.Parameters.Add("@FirmaDigital", SqlDbType.VarChar).Value = contenidoFirmaDigtal ;//724
                                sqldata04.Fill(dttContenido1);
                                con1.Close();
                            }


                        }


                        catch (Exception ex)
                        {
                            con1.Close();
                        }

                    }

                    catch (Exception ex)
                    {

                    }
                }

            }
            catch (Exception ex) { }


            //-----------------------  Consultar el CDR  --------------------------

            try
            {

                //Lista los documentos aptos para consultar su CDR

                var sqldatalistarEnvioCDR = new SqlDataAdapter("[dbo].[MSS_FE_LISTA_TODOS_DOC_ACEPTADOS_CDR]", con2);
                con2.Open();
                sqldatalistarEnvioCDR.SelectCommand.CommandType = CommandType.StoredProcedure;
                sqldatalistarEnvioCDR.Fill(dttListAllEnvioCDR);
                con2.Close();
                //dttListAllEnvioCDR
                docEntry = Convert.ToInt32(dttListAllEnvioCDR.Rows[0]["DocEntry"]);//Convert.ToInt64(dttListAllEnvioCDR.Rows[0]["DocEntry")) ;//
                objtype = Convert.ToString(dttListAllEnvioCDR.Rows[0]["ObjType"]);
                docsubtype = Convert.ToString(dttListAllEnvioCDR.Rows[0]["DocSubType"]);
                tipoDoc = Convert.ToString(dttListAllEnvioCDR.Rows[0]["TipoDoc"]);
                serie = Convert.ToString(dttListAllEnvioCDR.Rows[0]["Serie"]);
                correlativo = Convert.ToString(dttListAllEnvioCDR.Rows[0]["Correlativo"]);
                var consultarStatus = "220";
                var statuscdr = "";


                if (objtype == "13")
                {
                    try
                    {
                        if (tipoDoc == "01")
                        {

                            documentTypeWS = 1;// Atributo que se enviara por el WS

                            try
                            {

                                try
                                {

                                    IdentifierWS = serie + "-" + correlativo;

                                    //Inicio_autorizacion
                                    var credencialescon = new WSServillantas.authorization();
                                    credencialescon.user = WsEfactUserV;
                                    credencialescon.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var clientConsul = new WSServillantas.TransactionServiceClient();
                                    var responseConsultar = new WSServillantas.transactionResult();


                                    responseConsultar = clientConsul.getStatus(credencialescon, documentTypeWS, IdentifierWS);




                                    var contbase64Consultar = new Byte[] { };
                                    contbase64Consultar = responseConsultar.cdrFile;//ConvertFileToBase64(rutanombreEnviozip)
                                    statuscdr = responseConsultar.status;



                                    System.IO.File.WriteAllBytes("D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip", contbase64Consultar);
                                    var nameconsultarzip = "D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip";


                                    try
                                    {
                                        using (ZipFile zip = ZipFile.Read(nameconsultarzip))
                                        {
                                            foreach (ZipEntry ev in zip)
                                            {
                                                ev.Extract("D:\\JSOLISSEIDOR\\");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine("exception: {0}", ex.ToString());
                                    }


                                    consultarStatus = obtenerCDRSunat();



                                    var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_UPDATE_CONSULTAR]", con2);
                                    con2.Open();
                                    sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                    sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                    sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@StatusCdr", SqlDbType.VarChar).Value = consultarStatus;//724
                                    sqldata03.SelectCommand.Parameters.Add("@Status", SqlDbType.VarChar).Value = statuscdr;//724
                                                                                                                           //@Status
                                    sqldata03.Fill(dttContenido2);
                                    con2.Close();


                                    //-- Firma Digital no va.




                                    var prueba = "test";
                                }

                                catch (Exception ex)
                                {
                                    con1.Close();
                                }
                            }

                            catch (Exception ex)
                            {
                                con1.Close();
                                //fileReader.Close()
                            }

                        }

                        if (tipoDoc == "03")
                        {

                            try
                            {
                                documentTypeWS = 3;


                                try
                                {

                                    IdentifierWS = serie + "-" + correlativo;

                                    //Inicio_autorizacion
                                    var credencialescon = new WSServillantas.authorization();
                                    credencialescon.user = WsEfactUserV;
                                    credencialescon.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var clientConsul = new WSServillantas.TransactionServiceClient();
                                    var responseConsultar = new WSServillantas.transactionResult();


                                    responseConsultar = clientConsul.getStatus(credencialescon, documentTypeWS, IdentifierWS);



                                    var contbase64Consultar = new Byte[] { };
                                    contbase64Consultar = responseConsultar.cdrFile;//ConvertFileToBase64(rutanombreEnviozip)
                                    statuscdr = responseConsultar.status;


                                    System.IO.File.WriteAllBytes("D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip", contbase64Consultar);
                                    var nameconsultarzip = "D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip";




                                    try
                                    {
                                        using (ZipFile zipBoleta = ZipFile.Read(nameconsultarzip))
                                        {

                                            foreach (var ev in zipBoleta)
                                            {
                                                ev.Extract("D:\\JSOLISSEIDOR\\");
                                            }
                                        }
                                    }
                                    catch (Exception ex1)
                                    {
                                        Console.Error.WriteLine("exception: {0}", ex1.ToString());
                                    }

                                    consultarStatus = obtenerCDRSunat();


                                    var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_UPDATE_CONSULTAR]", con2);
                                    con2.Open();
                                    sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                    sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                    sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@StatusCdr", SqlDbType.VarChar).Value = consultarStatus;//724
                                    sqldata03.SelectCommand.Parameters.Add("@Status", SqlDbType.VarChar).Value = statuscdr;//724
                                                                                                                           //@Status
                                    sqldata03.Fill(dttContenido2);
                                    con2.Close();


                                    var prueba = "test";
                                }

                                catch (Exception ex)
                                {
                                    con1.Close();
                                }
                            }

                            catch (Exception ex)
                            {
                                //fileReader.Close()
                                con2.Close();
                            }

                        }

                        if (tipoDoc == "08")
                        {

                            try
                            {
                                documentTypeWS = 8;
                                try
                                {

                                    IdentifierWS = serie + "-" + correlativo;

                                    //Inicio_autorizacion
                                    var credencialescon = new WSServillantas.authorization();
                                    credencialescon.user = WsEfactUserV;
                                    credencialescon.password = WsEfactPasswordV;
                                    //Fin_autorizacion

                                    var clientConsul = new WSServillantas.TransactionServiceClient();
                                    var responseConsultar = new WSServillantas.transactionResult();


                                    responseConsultar = clientConsul.getStatus(credencialescon, documentTypeWS, IdentifierWS);

                                    var contbase64Consultar = new Byte[] { };
                                    contbase64Consultar = responseConsultar.cdrFile;//ConvertFileToBase64(rutanombreEnviozip)
                                    statuscdr = responseConsultar.status;


                                    System.IO.File.WriteAllBytes("D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip", contbase64Consultar);

                                    var nameconsultarzip = "D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip";

                                    try
                                    {
                                        using (ZipFile zip = ZipFile.Read(nameconsultarzip))
                                        {
                                            foreach (var ev in zip)
                                            {
                                                ev.Extract("D:\\JSOLISSEIDOR\\");
                                            }
                                        }
                                    }
                                    catch (Exception ex1)
                                    {
                                        Console.Error.WriteLine("exception: {0}", ex1.ToString());
                                    }


                                    consultarStatus = obtenerCDRSunat();
                                    //statuscdr       = 



                                    var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_UPDATE_CONSULTAR]", con2);
                                    con2.Open();
                                    sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                                    sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                                    sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                                    sqldata03.SelectCommand.Parameters.Add("@StatusCdr", SqlDbType.VarChar).Value = consultarStatus;//724
                                    sqldata03.SelectCommand.Parameters.Add("@Status", SqlDbType.VarChar).Value = statuscdr;//724
                                                                                                                           //@Status
                                    sqldata03.Fill(dttContenido2);
                                    con2.Close();


                                    var prueba = "test";
                                }
                                catch (Exception ex)
                                {
                                    con1.Close();
                                }
                            }

                            catch (Exception ex)
                            {
                                //fileReader.Close()
                                con2.Close();
                            }

                        }


                    }
                    catch (Exception ex)
                    {

                        con.Close();
                        //System.Windows.Forms.MessageBox("asdasd")
                    }
                }

                if (objtype == "14")
                {

                    try
                    {
                        documentTypeWS = 7;
                        try
                        {

                            IdentifierWS = serie + "-" + correlativo;

                            //Inicio_autorizacion
                            var credencialescon = new WSServillantas.authorization();
                            credencialescon.user = WsEfactUserV;
                            credencialescon.password = WsEfactPasswordV;
                            //Fin_autorizacion

                            var clientConsul = new WSServillantas.TransactionServiceClient();
                            var responseConsultar = new WSServillantas.transactionResult();


                            responseConsultar = clientConsul.getStatus(credencialescon, documentTypeWS, IdentifierWS);

                            var contbase64Consultar = new Byte[] { };
                            contbase64Consultar = responseConsultar.cdrFile;//ConvertFileToBase64(rutanombreEnviozip)
                            statuscdr = responseConsultar.status;


                            System.IO.File.WriteAllBytes("D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip", contbase64Consultar);
                            var nameconsultarzip = "D:\\JSOLISSEIDOR\\" + "0" + Convert.ToString(documentTypeWS) + " - " + IdentifierWS + ".zip";

                            try
                            {
                                using (var zip = ZipFile.Read(nameconsultarzip))
                                {
                                    foreach (var ev in zip)
                                    {
                                        ev.Extract("D:\\JSOLISSEIDOR\\");
                                    }
                                }
                            }
                            catch (Exception ex1)
                            {
                                Console.Error.WriteLine("exception: {0}", ex1.ToString());
                            }

                            consultarStatus = obtenerCDRSunat();



                            var sqldata03 = new SqlDataAdapter("[dbo].[MSS_FE_UPDATE_CONSULTAR]", con2);
                            con2.Open();
                            sqldata03.SelectCommand.CommandType = CommandType.StoredProcedure;
                            sqldata03.SelectCommand.Parameters.Add("@DocEntry", SqlDbType.Int).Value = docEntry;//724
                            sqldata03.SelectCommand.Parameters.Add("@ObjType", SqlDbType.VarChar).Value = objtype;//724
                            sqldata03.SelectCommand.Parameters.Add("@DocSubType", SqlDbType.VarChar).Value = docsubtype;//724
                            sqldata03.SelectCommand.Parameters.Add("@StatusCdr", SqlDbType.VarChar).Value = consultarStatus;//724
                            sqldata03.SelectCommand.Parameters.Add("@Status", SqlDbType.VarChar).Value = statuscdr;//724
                                                                                                                   //@Status
                            sqldata03.Fill(dttContenido2);
                            con2.Close();


                            var prueba = "test";
                        }
                        catch (Exception ex)
                        {
                            con1.Close();
                        }
                    }

                    catch (Exception ex)
                    {
                        //fileReader.Close()
                        con2.Close();
                    }
                }


            }

            catch (Exception ex)
            {

            }

        }

        //Method to compress.
        private void Compress(FileInfo fi)
        {
            //Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                //Compressing:
                //Prevent compressing hidden and already compressed files.
                if (File.GetAttributes(fi.FullName) != FileAttributes.Hidden && fi.Extension != ".zip")
                {
                    //Create the compressed file.
                    using (FileStream outFile = File.Create(fi.FullName + ".zip"))
                    {
                        using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                        {
                            //Copy the source file into the compression stream.
                            inFile.CopyTo(Compress);
                            Console.WriteLine("Compressed {0} from {1} to {2} bytes.", fi.Name, fi.Length.ToString(), outFile.Length.ToString());
                        }
                    }

                }
            }
        }

        public Byte[] ConvertFileArraybyterutanombreEnviozipw(string fileName) => File.ReadAllBytes(fileName);

        public Byte[] ConvertFileToBase64(string fileName) => System.IO.File.ReadAllBytes(fileName);

        public string obtenerCDRSunat()
        {

            String nombrezip;


            string serie = "";
            string codigo = "";
            string dec = "";
            string mensaje = "";

            try
            {

                var directoriobase = new DirectoryInfo("D:\\JSOLISSEIDOR\\");

                foreach (var file in directoriobase.GetFiles())
                {

                    nombrezip = file.FullName;




                    if (nombrezip.Contains(".xml"))
                    {

                        string ruta = "";
                        ruta = nombrezip;

                        var xmldoc = new XmlDocument();
                        xmldoc.Load(ruta);

                        try
                        {

                            XmlNodeList xlista = xmldoc.GetElementsByTagName("ApplicationResponse");
                            var elem = xlista[0] as XmlElement;
                            XmlNodeList xlista1 = elem.GetElementsByTagName("cac:DocumentResponse");

                            XmlElement elem1 = xlista1[0] as XmlElement;
                            XmlNodeList xlista2 = elem1.GetElementsByTagName("cac:Response");

                            foreach (XmlElement nodo in xlista2)
                            {
                                XmlNodeList nombre = nodo.GetElementsByTagName("cbc:ReferenceID");
                                XmlNodeList ncode = nodo.GetElementsByTagName("cbc:ResponseCode");
                                XmlNodeList ndescripcion = nodo.GetElementsByTagName("cbc:Description");

                                serie = nombre[0].InnerText;
                                codigo = ncode[0].InnerText;
                                dec = ndescripcion[0].InnerText;

                            };

                            //My.Computer.FileSystem.DeleteFile(ruta,
                            //            Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
                            //            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin)


                            System.IO.File.Delete(ruta);

                            if (codigo == "0")
                                mensaje = "aceptado";

                            if (codigo == "1")
                                mensaje = "rechazado";

                            if (codigo == "2")
                                mensaje = "exception";

                            if (codigo == "3")
                                mensaje = "acecptado con observaciones";
                        }

                        catch (Exception ex)
                        {
                            System.IO.File.Delete(ruta);

                        }


                    }



                }
            }

            catch (Exception ex) { }


            return mensaje;
        }
    }
}







