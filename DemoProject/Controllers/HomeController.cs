using DemoProject.Models;
using DemoProject.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Data.SqlClient;
using OfficeOpenXml;

namespace DemoProject.Controllers
{
    public class HomeController : Controller
    {
        public static string connectionStr = "Server=LAPTOP-NNNR9RGP\\SQLEXPRESS;Database=GSB;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<ApplicationForm> applicationList = new();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                string query = @"SELECT * FROM BasvuruFormu";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        applicationList.Add(new ApplicationForm
                        {
                            Id = (int)reader["Id"],
                            projectName = reader["ProjeAdi"].ToString(),
                            applicantUnit = reader["BasvuranBirim"].ToString(),
                            appliedProject = reader["BasvuruYapilanProje"].ToString(),
                            appliedType = reader["BasvuruYapilanTur"].ToString(),
                            participantType = reader["KatilimciTuru"].ToString(),
                            applicationPeriod = reader["BasvuruDonemi"].ToString(),
                            applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                            applicationState = reader["BasvuruDurumu"].ToString(),
                            stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                            grantAmount = reader["HibeTutari"] != DBNull.Value ? Convert.ToDecimal(reader["HibeTutari"]) : 0
                        });
                    }
                }
            }

           TableViewModel viewModel = new TableViewModel
            {
                Applications = applicationList,
                CurrentPage = 1, 
                TotalPages = 1
            };

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult sendApplicationForm([FromBody] ApplicationForm model)
        {
            try
            {
                Debug.WriteLine($"Project Name: {model.projectName}");
                Debug.WriteLine($"Applicant Unit: {model.applicantUnit}");
                Debug.WriteLine($"Applied Project: {model.appliedProject}");
                Debug.WriteLine($"Applied Type: {model.appliedType}");
                Debug.WriteLine($"Participant Type: {model.participantType}");
                Debug.WriteLine($"Application Period: {model.applicationPeriod}");
                Debug.WriteLine($"Application Date: {model.applicationDate:dd/MM/yyyy}");
                Debug.WriteLine($"Application State: {model.applicationState}");
                Debug.WriteLine($"State Date: {model.stateDate:dd/MM/yyyy}");
                Debug.WriteLine($"Grant Amount: {model.grantAmount}");

                using (SqlConnection sqlConnection = new SqlConnection(connectionStr))
                {
                    string insertQuery = @"
                        INSERT INTO BasvuruFormu 
                        (ProjeAdi, BasvuranBirim, BasvuruYapilanProje, BasvuruYapilanTur, 
                         KatilimciTuru, BasvuruDonemi, BasvuruTarihi, BasvuruDurumu, 
                         DurumTarihi, HibeTutari) 
                        VALUES 
                        (@ProjeAdi, @BasvuranBirim, @BasvuruYapilanProje, @BasvuruYapilanTur, 
                         @KatilimciTuru, @BasvuruDonemi, @BasvuruTarihi, @BasvuruDurumu, 
                         @DurumTarihi, @HibeTutari)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@ProjeAdi", model.projectName ?? "");
                        cmd.Parameters.AddWithValue("@BasvuranBirim", model.applicantUnit ?? "");
                        cmd.Parameters.AddWithValue("@BasvuruYapilanProje", model.appliedProject ?? "");
                        cmd.Parameters.AddWithValue("@BasvuruYapilanTur", model.appliedType ?? "");
                        cmd.Parameters.AddWithValue("@KatilimciTuru", model.participantType ?? "");
                        cmd.Parameters.AddWithValue("@BasvuruDonemi", model.applicationPeriod ?? "");
                        cmd.Parameters.AddWithValue("@BasvuruTarihi", model.applicationDate);
                        cmd.Parameters.AddWithValue("@BasvuruDurumu", model.applicationState ?? "");
                        cmd.Parameters.AddWithValue("@DurumTarihi", model.stateDate);
                        cmd.Parameters.AddWithValue("@HibeTutari", model.grantAmount ?? (object)DBNull.Value);

                        sqlConnection.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        Debug.WriteLine($"Rows affected: {rowsAffected}");
                    }
                }

                return Json(new { success = true, message = "Başvuru başarıyla kaydedildi!" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        [HttpGet]
        public JsonResult GetApplicationsPaged(int page = 1, int pageSize = 10, int? status = null) 
        {
            List<ApplicationForm> basvuruList = new List<ApplicationForm>();
            int totalCount = 0;

            using (System.Data.SqlClient.SqlConnection sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                try
                {
                    sqlConnection1.Open();

                    int offset = (page - 1) * pageSize;
                    int startRow = offset + 1;
                    int endRow = offset + pageSize;

                    string countQueryText = "SELECT COUNT(*) FROM BasvuruFormu WHERE (@Status IS NULL OR BasvuruDurumu = @Status)";
                    using (var countCmd = new System.Data.SqlClient.SqlCommand(countQueryText, sqlConnection1))
                    {
                        countCmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                        totalCount = (int)countCmd.ExecuteScalar();
                    }

                    string queryText = @"
                SELECT * FROM (
                    SELECT ROW_NUMBER() OVER (ORDER BY Id ASC) AS RowNum, 
                           Id, ProjeAdi, BasvuranBirim, BasvuruYapilanProje, BasvuruYapilanTur,
                           KatilimciTuru, BasvuruDonemi, BasvuruTarihi, BasvuruDurumu, 
                           DurumTarihi, HibeTutari
                    FROM BasvuruFormu
                    WHERE (@Status IS NULL OR BasvuruDurumu = @Status)
                ) AS Temp
                WHERE RowNum BETWEEN @StartRow AND @EndRow";

                    using (var query = new System.Data.SqlClient.SqlCommand(queryText, sqlConnection1))
                    {
                        query.CommandType = System.Data.CommandType.Text;
                        query.Parameters.AddWithValue("@StartRow", startRow);
                        query.Parameters.AddWithValue("@EndRow", endRow);
                        query.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

                        using (var reader = query.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                basvuruList.Add(new ApplicationForm
                                {
                                    Id = Convert.ToInt32(reader["Id"]), 
                                    projectName = reader["ProjeAdi"].ToString(),
                                    applicantUnit = reader["BasvuranBirim"].ToString(),
                                    appliedProject = reader["BasvuruYapilanProje"].ToString(),
                                    appliedType = reader["BasvuruYapilanTur"].ToString(),
                                    participantType = reader["KatilimciTuru"].ToString(),
                                    applicationPeriod = reader["BasvuruDonemi"].ToString(),
                                    applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                                    applicationState = reader["BasvuruDurumu"].ToString(),
                                    stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                                    grantAmount = Convert.ToDecimal(reader["HibeTutari"])
                                });
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        data = basvuruList,
                        totalCount = totalCount
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Hata: " + ex.Message });
                }
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult GetReferences() 
        {
            try
            {
                List<Reference> references = new List<Reference>();

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string checkSql = "SELECT COUNT(*) FROM Reference WHERE IsActive = 1";
                    SqlCommand checkCmd = new SqlCommand(checkSql, conn);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count == 0)
                    {
                        AddDefaultReferenceData();
                    }

                    string sql = "SELECT Id, ReferenceType, ReferenceKey, ReferenceValue, IsActive FROM Reference WHERE IsActive = 1 ORDER BY ReferenceType, ReferenceValue";
                    SqlCommand cmd = new SqlCommand(sql, conn);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            references.Add(new Reference
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ReferenceType = reader["ReferenceType"].ToString(),
                                ReferenceKey = reader["ReferenceKey"].ToString(),
                                ReferenceValue = reader["ReferenceValue"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }

                return Json(new { success = true, data = references });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading references");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void AddDefaultReferenceData()
        {
            var referenceData = new List<Reference>
    {
        new Reference { ReferenceType = "BASVURAN_BIRIM", ReferenceKey = "bilgi-islem", ReferenceValue = "Bilgi İşlem", IsActive = true },
        new Reference { ReferenceType = "BASVURAN_BIRIM", ReferenceKey = "insan-kaynaklari", ReferenceValue = "İnsan Kaynakları", IsActive = true },
        new Reference { ReferenceType = "BASVURAN_BIRIM", ReferenceKey = "yatirim-isleri", ReferenceValue = "Yatırım İşleri", IsActive = true },
        new Reference { ReferenceType = "BASVURAN_BIRIM", ReferenceKey = "test", ReferenceValue = "Test", IsActive = true },

        new Reference { ReferenceType = "BASVURU_PROJE", ReferenceKey = "erasmus", ReferenceValue = "Erasmus", IsActive = true },
        new Reference { ReferenceType = "BASVURU_PROJE", ReferenceKey = "merkezi", ReferenceValue = "Merkezi", IsActive = true },
        new Reference { ReferenceType = "BASVURU_PROJE", ReferenceKey = "avrupa", ReferenceValue = "Avrupa", IsActive = true },
        new Reference { ReferenceType = "BASVURU_PROJE", ReferenceKey = "diger", ReferenceValue = "Diğer", IsActive = true },

        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "genclik", ReferenceValue = "Gençlik", IsActive = true },
        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "yetiskin", ReferenceValue = "Yetişkin", IsActive = true },
        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "spor", ReferenceValue = "Spor", IsActive = true },
        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "mesleki", ReferenceValue = "Mesleki", IsActive = true },
        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "dijital", ReferenceValue = "Dijital", IsActive = true },
        new Reference { ReferenceType = "BASVURU_TUR", ReferenceKey = "diger-tur", ReferenceValue = "Diğer", IsActive = true },

        new Reference { ReferenceType = "KATILIMCI_TURU", ReferenceKey = "koordinator", ReferenceValue = "Koordinatör", IsActive = true },
        new Reference { ReferenceType = "KATILIMCI_TURU", ReferenceKey = "ortak", ReferenceValue = "Ortak", IsActive = true },
        new Reference { ReferenceType = "KATILIMCI_TURU", ReferenceKey = "r2", ReferenceValue = "R2", IsActive = true },
        new Reference { ReferenceType = "KATILIMCI_TURU", ReferenceKey = "r3", ReferenceValue = "R3", IsActive = true },

        new Reference { ReferenceType = "BASVURU_DONEMI", ReferenceKey = "r1", ReferenceValue = "R1", IsActive = true },
        new Reference { ReferenceType = "BASVURU_DONEMI", ReferenceKey = "r2", ReferenceValue = "R2", IsActive = true },
        new Reference { ReferenceType = "BASVURU_DONEMI", ReferenceKey = "r3", ReferenceValue = "R3", IsActive = true },

        new Reference { ReferenceType = "BASVURU_DURUMU", ReferenceKey = "kabul", ReferenceValue = "Kabul", IsActive = true },
        new Reference { ReferenceType = "BASVURU_DURUMU", ReferenceKey = "red", ReferenceValue = "Red", IsActive = true },
    };

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                foreach (var data in referenceData)
                {
                    string sql = "INSERT INTO Reference (ReferenceType, ReferenceKey, ReferenceValue, IsActive) VALUES (@ReferenceType, @ReferenceKey, @ReferenceValue, @IsActive)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ReferenceType", data.ReferenceType);
                    cmd.Parameters.AddWithValue("@ReferenceKey", data.ReferenceKey);
                    cmd.Parameters.AddWithValue("@ReferenceValue", data.ReferenceValue);
                    cmd.Parameters.AddWithValue("@IsActive", data.IsActive);

                    cmd.ExecuteNonQuery();
                }
            }

            _logger.LogInformation($"Added {referenceData.Count} default reference records to database");
        }


        public IActionResult Tables(int page = 1)
        {
            int pageSize = 10;
            int skip = (page - 1) * pageSize;

            List<ApplicationForm> applications = new List<ApplicationForm>();
            List<Reference> references = new List<Reference>();
            int totalCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM BasvuruFormu", conn);
                totalCount = (int)countCmd.ExecuteScalar();

                string sql = @"
            SELECT * FROM BasvuruFormu
            ORDER BY Id
            OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Skip", skip);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        applications.Add(new ApplicationForm
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            projectName = reader["ProjeAdi"].ToString(),
                            applicantUnit = reader["BasvuranBirim"].ToString(),
                            appliedProject = reader["BasvuruYapilanProje"].ToString(),
                            appliedType = reader["BasvuruYapilanTur"].ToString(),
                            participantType = reader["KatilimciTuru"].ToString(),
                            applicationPeriod = reader["BasvuruDonemi"].ToString(),
                            applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                            applicationState = reader["BasvuruDurumu"].ToString(),
                            stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                            grantAmount = Convert.ToDecimal(reader["HibeTutari"]),
                        });
                    }
                }

                SqlCommand refCmd = new SqlCommand(@"
            SELECT Id, ReferenceType, ReferenceKey, ReferenceValue, IsActive
            FROM Reference
            WHERE IsActive = 1
            ORDER BY ReferenceType, ReferenceValue", conn);

                using (SqlDataReader refReader = refCmd.ExecuteReader())
                {
                    while (refReader.Read())
                    {
                        references.Add(new Reference
                        {
                            Id = Convert.ToInt32(refReader["Id"]),
                            ReferenceType = refReader["ReferenceType"].ToString(),
                            ReferenceKey = refReader["ReferenceKey"].ToString(),
                            ReferenceValue = refReader["ReferenceValue"].ToString(),
                            IsActive = Convert.ToBoolean(refReader["IsActive"])
                        });
                    }
                }
            }

            var viewModel = new TableViewModel
            {
                Applications = applications,
                Reference = references,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return View(viewModel);
        }

        [HttpGet]
        public JsonResult GetReferenceOptions(string referenceType) 
        {
            List<string> options = new();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = @"SELECT ReferenceValue FROM Reference 
                         WHERE ReferenceType = @type AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@type", referenceType);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            options.Add(reader["ReferenceValue"].ToString());
                        }
                    }
                }
            }

            return Json(options);
        }


        [HttpGet]
        public JsonResult FilterApplicationsPaged(int page = 1, int pageSize = 10,
            string? projectName = null, string? applicantUnit = null, string? appliedProject = null,
            string? appliedType = null, string? participantType = null, string? applicationPeriod = null,
            DateTime? applicationDate = null, string? applicationState = null,
            DateTime? stateDate = null, decimal? grantAmount = null) 
        {
            List<ApplicationForm> filtered = new();
            int totalCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                string baseSql = @"
        SELECT *, ROW_NUMBER() OVER (ORDER BY Id) AS RowNum 
        FROM BasvuruFormu
        WHERE (@projectName IS NULL OR ProjeAdi LIKE '%' + @projectName + '%')
          AND (@applicantUnit IS NULL OR BasvuranBirim = @applicantUnit)
          AND (@appliedProject IS NULL OR BasvuruYapilanProje = @appliedProject)
          AND (@appliedType IS NULL OR BasvuruYapilanTur = @appliedType)
          AND (@participantType IS NULL OR KatilimciTuru = @participantType)
          AND (@applicationPeriod IS NULL OR BasvuruDonemi = @applicationPeriod)
          AND (@applicationDate IS NULL OR CAST(BasvuruTarihi AS DATE) = @applicationDate)
          AND (@applicationState IS NULL OR BasvuruDurumu = @applicationState)
          AND (@stateDate IS NULL OR CAST(DurumTarihi AS DATE) = @stateDate)
          AND (@grantAmount IS NULL OR HibeTutari = @grantAmount)";

                string pagedSql = $@"
        SELECT * FROM ({baseSql}) AS T
        WHERE RowNum BETWEEN @StartRow AND @EndRow";

                string countSql = $"SELECT COUNT(*) FROM ({baseSql}) AS CountTable";

                using (SqlCommand countCmd = new SqlCommand(countSql, conn))
                using (SqlCommand dataCmd = new SqlCommand(pagedSql, conn))
                {
                    object[] allParams = {
                        projectName,
                        applicantUnit,
                        appliedProject,
                        appliedType,
                        participantType,
                        applicationPeriod,
                        applicationDate,
                        applicationState,
                        stateDate,
                        grantAmount
                    };

                    string[] names = { "projectName", "applicantUnit", "appliedProject", "appliedType", "participantType", "applicationPeriod", "applicationDate", "applicationState", "stateDate", "grantAmount" };

                    for (int i = 0; i < names.Length; i++)
                    {
                        countCmd.Parameters.AddWithValue("@" + names[i], (object?)allParams[i] ?? DBNull.Value);
                        dataCmd.Parameters.AddWithValue("@" + names[i], (object?)allParams[i] ?? DBNull.Value);
                    }

                    int startRow = ((page - 1) * pageSize) + 1;
                    int endRow = startRow + pageSize - 1;
                    dataCmd.Parameters.AddWithValue("@StartRow", startRow);
                    dataCmd.Parameters.AddWithValue("@EndRow", endRow);

                    totalCount = (int)countCmd.ExecuteScalar();

                    using (var reader = dataCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            filtered.Add(new ApplicationForm
                            {
                                projectName = reader["ProjeAdi"].ToString(),
                                applicantUnit = reader["BasvuranBirim"].ToString(),
                                appliedProject = reader["BasvuruYapilanProje"].ToString(),
                                appliedType = reader["BasvuruYapilanTur"].ToString(),
                                participantType = reader["KatilimciTuru"].ToString(),
                                applicationPeriod = reader["BasvuruDonemi"].ToString(),
                                applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                                applicationState = reader["BasvuruDurumu"].ToString(),
                                stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                                grantAmount = reader["HibeTutari"] != DBNull.Value ? Convert.ToDecimal(reader["HibeTutari"]) : null
                            });
                        }
                    }
                }
            }

            return Json(new { data = filtered, totalCount = totalCount });
        }

        [HttpGet]
        public JsonResult GetReferenceList()
        {
            List<Reference> referenceList = new List<Reference>();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = @"
            SELECT 
                Id,
                ReferenceType,
                ReferenceKey,
                ReferenceValue,
                IsActive,
                CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Deleted' END AS Status
            FROM Reference
            ORDER BY ReferenceType, ReferenceValue";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Reference refItem = new Reference
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReferenceType = reader["ReferenceType"].ToString(),
                            ReferenceKey = reader["ReferenceKey"].ToString(),
                            ReferenceValue = reader["ReferenceValue"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                        };

                        referenceList.Add(refItem);
                    }
                }
            }

            return Json(referenceList);
        }

        [HttpGet]
        public IActionResult ExportToExcel(string? projectName, string? applicantUnit, string? appliedProject,
                                     string? appliedType, string? participantType, string? applicationPeriod,
                                     DateTime? applicationDate, string? applicationState,
                                     DateTime? stateDate, decimal? grantAmount)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            List<ApplicationForm> basvurular = new();

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                var query = @"
            SELECT * FROM BasvuruFormu WHERE 1=1
                " + (string.IsNullOrEmpty(projectName) ? "" : " AND ProjeAdi = @projectName") + @"
                " + (string.IsNullOrEmpty(applicantUnit) ? "" : " AND BasvuranBirim = @applicantUnit") + @"
                " + (string.IsNullOrEmpty(appliedProject) ? "" : " AND BasvuruYapilanProje = @appliedProject") + @"
                " + (string.IsNullOrEmpty(appliedType) ? "" : " AND BasvuruYapilanTur = @appliedType") + @"
                " + (string.IsNullOrEmpty(participantType) ? "" : " AND KatilimciTuru = @participantType") + @"
                " + (string.IsNullOrEmpty(applicationPeriod) ? "" : " AND BasvuruDonemi = @applicationPeriod") + @"
                " + (applicationDate == null ? "" : " AND BasvuruTarihi = @applicationDate") + @"
                " + (string.IsNullOrEmpty(applicationState) ? "" : " AND BasvuruDurumu = @applicationState") + @"
                " + (stateDate == null ? "" : " AND DurumTarihi = @stateDate") + @"
                " + (grantAmount == null ? "" : " AND HibeTutari = @grantAmount");

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {

                    if (!string.IsNullOrEmpty(projectName)) cmd.Parameters.AddWithValue("@projectName", projectName);
                    if (!string.IsNullOrEmpty(applicantUnit)) cmd.Parameters.AddWithValue("@applicantUnit", applicantUnit);
                    if (!string.IsNullOrEmpty(appliedProject)) cmd.Parameters.AddWithValue("@appliedProject", appliedProject);
                    if (!string.IsNullOrEmpty(appliedType)) cmd.Parameters.AddWithValue("@appliedType", appliedType);
                    if (!string.IsNullOrEmpty(participantType)) cmd.Parameters.AddWithValue("@participantType", participantType);
                    if (!string.IsNullOrEmpty(applicationPeriod)) cmd.Parameters.AddWithValue("@applicationPeriod", applicationPeriod);
                    if (applicationDate != null) cmd.Parameters.AddWithValue("@applicationDate", applicationDate);
                    if (!string.IsNullOrEmpty(applicationState)) cmd.Parameters.AddWithValue("@applicationState", applicationState);
                    if (stateDate != null) cmd.Parameters.AddWithValue("@stateDate", stateDate);
                    if (grantAmount != null) cmd.Parameters.AddWithValue("@grantAmount", grantAmount);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            basvurular.Add(new ApplicationForm
                            {
                                projectName = reader["ProjeAdi"].ToString(),
                                applicantUnit = reader["BasvuranBirim"].ToString(),
                                appliedProject = reader["BasvuruYapilanProje"].ToString(),
                                appliedType = reader["BasvuruYapilanTur"].ToString(),
                                participantType = reader["KatilimciTuru"].ToString(),
                                applicationPeriod = reader["BasvuruDonemi"].ToString(),
                                applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                                applicationState = reader["BasvuruDurumu"].ToString(),
                                stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                                grantAmount = Convert.ToDecimal(reader["HibeTutari"])
                            });
                        }
                    }
                }
            }

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Başvurular");


                sheet.Cells[1, 1].Value = "PROJE ADI";
                sheet.Cells[1, 2].Value = "BAŞVURAN BİRİM";
                sheet.Cells[1, 3].Value = "BAŞVURU YAPILAN PROJE";
                sheet.Cells[1, 4].Value = "BAŞVURU TÜRÜ";
                sheet.Cells[1, 5].Value = "KATILIMCI TÜRÜ";
                sheet.Cells[1, 6].Value = "BAŞVURU DÖNEMİ";
                sheet.Cells[1, 7].Value = "BAŞVURU TARİHİ";
                sheet.Cells[1, 8].Value = "BAŞVURU DURUMU";
                sheet.Cells[1, 9].Value = "DURUM TARİHİ";
                sheet.Cells[1, 10].Value = "HİBE TUTARI";

                for (int i = 0; i < basvurular.Count; i++)
                {
                    var b = basvurular[i];
                    sheet.Cells[i + 2, 1].Value = b.projectName;
                    sheet.Cells[i + 2, 2].Value = b.applicantUnit;
                    sheet.Cells[i + 2, 3].Value = b.appliedProject;
                    sheet.Cells[i + 2, 4].Value = b.appliedType;
                    sheet.Cells[i + 2, 5].Value = b.participantType;
                    sheet.Cells[i + 2, 6].Value = b.applicationPeriod;
                    sheet.Cells[i + 2, 7].Value = b.applicationDate.ToShortDateString();
                    sheet.Cells[i + 2, 8].Value = b.applicationState;
                    sheet.Cells[i + 2, 9].Value = b.stateDate.ToShortDateString();
                    sheet.Cells[i + 2, 10].Value = b.grantAmount;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"BasvuruListesi_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
        }

        [HttpPost]
        public JsonResult SoftDeleteApplication([FromBody] DeleteRequestDto request)
        {
            try
            {
                int id = request.id;
                _logger.LogInformation($"SoftDeleteApplication çağrıldı. ID: {id}");

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM BasvuruFormu WHERE Id = @Id";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            return Json(new { success = false, message = $"ID {id} ile başvuru bulunamadı." });
                        }
                    }

                    string deleteQuery = "DELETE FROM BasvuruFormu WHERE Id = @Id";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", id);
                        int affectedRows = deleteCmd.ExecuteNonQuery();

                        if (affectedRows > 0)
                        {
                            return Json(new { success = true, message = "Başvuru başarıyla silindi." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Silme işlemi başarısız." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SoftDeleteApplication hatası. ID: {request?.id}");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        public class DeleteRequestDto
        {
            public int id { get; set; }
        }

        [HttpPost]
        public JsonResult UpdateApplication(ApplicationForm form) 
        {
            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = @"UPDATE BasvuruFormu SET
                ProjeAdi = @projectName,
                BasvuranBirim = @applicantUnit,
                BasvuruYapilanProje = @appliedProject,
                BasvuruYapilanTur = @appliedType,
                KatilimciTuru = @participantType,
                BasvuruDonemi = @applicationPeriod,
                BasvuruTarihi = @applicationDate,
                BasvuruDurumu = @applicationState,
                DurumTarihi = @stateDate,
                HibeTutari = @grantAmount
                WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectName", form.projectName);
                    cmd.Parameters.AddWithValue("@applicantUnit", form.applicantUnit);
                    cmd.Parameters.AddWithValue("@appliedProject", form.appliedProject); 
                    cmd.Parameters.AddWithValue("@appliedType", form.appliedType); 
                    cmd.Parameters.AddWithValue("@participantType", form.participantType); 
                    cmd.Parameters.AddWithValue("@applicationPeriod", form.applicationPeriod); 
                    cmd.Parameters.AddWithValue("@applicationDate", form.applicationDate); 
                    cmd.Parameters.AddWithValue("@applicationState", form.applicationState); 
                    cmd.Parameters.AddWithValue("@stateDate", form.stateDate); 
                    cmd.Parameters.AddWithValue("@grantAmount", form.grantAmount); 
                    cmd.Parameters.AddWithValue("@Id", form.Id); 

                    int affectedRows = cmd.ExecuteNonQuery();
                    return Json(new { success = affectedRows > 0 });
                }
            }
        }

        [HttpGet]
        public JsonResult GetApplicationById(int id) 
        {
            ApplicationForm form = null;

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = "SELECT * FROM BasvuruFormu WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    form = new ApplicationForm
                    {
                        Id = (int)reader["Id"],
                        projectName = reader["ProjeAdi"].ToString(),
                        applicantUnit = reader["BasvuranBirim"].ToString(),
                        appliedProject = reader["BasvuruYapilanProje"].ToString(),
                        appliedType = reader["BasvuruYapilanTur"].ToString(),
                        participantType = reader["KatilimciTuru"].ToString(),
                        applicationPeriod = reader["BasvuruDonemi"].ToString(),
                        applicationDate = Convert.ToDateTime(reader["BasvuruTarihi"]),
                        applicationState = reader["BasvuruDurumu"].ToString(),
                        stateDate = Convert.ToDateTime(reader["DurumTarihi"]),
                        grantAmount = Convert.ToDecimal(reader["HibeTutari"])
                    };
                }
            }

            return Json(form);
        }

        public IActionResult AddReference()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetReferenceTypes() 
        {
            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT DISTINCT ReferenceType FROM Reference WHERE IsActive = 1", conn);

                var types = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        types.Add(reader["ReferenceType"].ToString());
                    }
                }
                return Json(types);
            }
        }

        [HttpGet]
        public IActionResult GetReferencesByType(string type)
        {
            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT ReferenceKey, ReferenceValue FROM Reference WHERE IsActive = 1 AND ReferenceType = @type", conn);
                cmd.Parameters.AddWithValue("@type", type);

                var list = new List<object>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            ReferenceKey = reader["ReferenceKey"].ToString(),
                            ReferenceValue = reader["ReferenceValue"].ToString()
                        });
                    }
                }

                return Json(list);
            }
        }

        [HttpPost]
        public IActionResult AddReference([FromBody] Reference model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ReferenceType) ||
                    string.IsNullOrEmpty(model.ReferenceKey) ||
                    string.IsNullOrEmpty(model.ReferenceValue))
                {
                    return BadRequest("Eksik alanlar var.");
                }

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    // Duplicate check
                    SqlCommand checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Reference 
                WHERE ReferenceType = @type AND ReferenceKey = @key", conn);
                    checkCmd.Parameters.AddWithValue("@type", model.ReferenceType);
                    checkCmd.Parameters.AddWithValue("@key", model.ReferenceKey);

                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0)
                        return Conflict("Bu referans anahtarı zaten mevcut.");

                    // Insert ve ID döndür
                    SqlCommand cmd = new SqlCommand(@"
                INSERT INTO Reference (ReferenceType, ReferenceKey, ReferenceValue, IsActive)
                VALUES (@type, @key, @value, @active);
                SELECT SCOPE_IDENTITY();", conn);

                    cmd.Parameters.AddWithValue("@type", model.ReferenceType);
                    cmd.Parameters.AddWithValue("@key", model.ReferenceKey);
                    cmd.Parameters.AddWithValue("@value", model.ReferenceValue);
                    cmd.Parameters.AddWithValue("@active", model.IsActive ? 1 : 0);

                    var newId = cmd.ExecuteScalar();

                    if (newId != null)
                    {
                        // Konsola yazdır (basit log)
                        Console.WriteLine($"Referans eklendi - ID: {newId}, Type: {model.ReferenceType}, Key: {model.ReferenceKey}");

                        return Ok(new
                        {
                            message = "Referans başarıyla eklendi.",
                            id = newId,
                            success = true
                        });
                    }
                    else
                    {
                        Console.WriteLine("Referans ekleme başarısız!");
                        return StatusCode(500, "Ekleme sırasında hata oluştu.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return StatusCode(500, $"Hata: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetAllReferences()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Id, ReferenceType, ReferenceKey, ReferenceValue, IsActive FROM Reference ORDER BY ReferenceType, ReferenceValue", conn);

                    var references = new List<object>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            references.Add(new
                            {
                                Id = reader["Id"],
                                ReferenceType = reader["ReferenceType"],
                                ReferenceKey = reader["ReferenceKey"],
                                ReferenceValue = reader["ReferenceValue"],
                                IsActive = reader["IsActive"]
                            });
                        }
                    }

                    Console.WriteLine($"{references.Count} referans bulundu");
                    return Json(references);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Referanslar getirilemedi: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult ReferenceOperations(int page = 1)
        {
            int pageSize = 10;
            int skip = (page - 1) * pageSize;

            List<Reference> referenceList = new List<Reference>();
            int totalCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();

                using (SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM Reference WHERE IsActive = 1", conn))
                {
                    totalCount = (int)countCmd.ExecuteScalar();
                }

                string sql = @"
            SELECT Id, ReferenceType, ReferenceKey, ReferenceValue, IsActive
            FROM Reference
            WHERE IsActive = 1
            ORDER BY ReferenceType, ReferenceValue
            OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            referenceList.Add(new Reference
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ReferenceType = reader["ReferenceType"].ToString(),
                                ReferenceKey = reader["ReferenceKey"].ToString(),
                                ReferenceValue = reader["ReferenceValue"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }

            var viewModel = new TableViewModel
            {
                Reference = referenceList, 
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return View(viewModel);
        }


        [HttpGet]
        public JsonResult GetReferencesPaged(int page = 1, int pageSize = 10, string referenceType = "") 
        {
            try
            {
                int skip = (page - 1) * pageSize;
                int totalCount = 0;
                List<Reference> data = new List<Reference>();

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string whereClause = "WHERE IsActive = 1";
                    bool hasReferenceType = !string.IsNullOrWhiteSpace(referenceType);

                    if (hasReferenceType)
                    {
                        whereClause += " AND ReferenceType = @ReferenceType";
                    }

                    string countQuery = $"SELECT COUNT(*) FROM Reference {whereClause}";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        if (hasReferenceType)
                        {
                            countCmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                        }

                        totalCount = (int)countCmd.ExecuteScalar();
                    }

                    string dataQuery = $@"
                SELECT Id, ReferenceType, ReferenceKey, ReferenceValue, IsActive
                FROM Reference
                {whereClause}
                ORDER BY Id
                OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(dataQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Skip", skip);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        if (hasReferenceType)
                        {
                            cmd.Parameters.AddWithValue("@ReferenceType", referenceType);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new Reference
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    ReferenceType = reader["ReferenceType"].ToString(),
                                    ReferenceKey = reader["ReferenceKey"].ToString(),
                                    ReferenceValue = reader["ReferenceValue"].ToString(),
                                    IsActive = Convert.ToBoolean(reader["IsActive"])
                                });
                            }
                        }
                    }
                }

                return Json(new { data = data, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.ToString() });
            }
        }


        [HttpPost]
        public JsonResult SoftDeleteReference(int id) 
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM Reference WHERE Id = @Id AND IsActive = 1";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        int existingCount = (int)checkCmd.ExecuteScalar();

                        if (existingCount == 0)
                        {
                            return Json(new { success = false, message = "Aktif referans bulunamadı." });
                        }
                    }

                    string updateQuery = "UPDATE Reference SET IsActive = 0 WHERE Id = @Id";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@Id", id);
                        int affectedRows = updateCmd.ExecuteNonQuery();

                        if (affectedRows > 0)
                        {
                            return Json(new { success = true, message = "Referans başarıyla silindi." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Silme işlemi başarısız." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(User loginUser)
        {
            if (string.IsNullOrEmpty(loginUser.EPosta) || string.IsNullOrEmpty(loginUser.Parola))
            {
                ViewBag.Error = "Lütfen e-posta ve parola alanlarını doldurun.";
                return View();
            }

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = @"SELECT COUNT(*) FROM [User] 
                        WHERE EPosta = @eposta AND Parola = @parola";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@eposta", loginUser.EPosta);
                    cmd.Parameters.AddWithValue("@parola", loginUser.Parola);

                    int userCount = (int)cmd.ExecuteScalar();

                    if (userCount > 0)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.Error = "Geçersiz e-posta veya parola.";
                        return View();
                    }
                }
            }
        }

        // Bonus: Kullanıcı bilgilerini almak için gelişmiş versiyon
        //[HttpPost]
        //public IActionResult LoginAdvanced(User loginUser)
        //{
        //    if (string.IsNullOrEmpty(loginUser.EPosta) || string.IsNullOrEmpty(loginUser.Parola))
        //    {
        //        ViewBag.Error = "Lütfen e-posta ve parola alanlarını doldurun.";
        //        return View("Login");
        //    }

        //    using (SqlConnection conn = new SqlConnection(connectionStr))
        //    {
        //        conn.Open();
        //        string query = @"SELECT KullaniciAdi, EPosta FROM [User] 
        //                WHERE EPosta = @eposta AND Parola = @parola";

        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@eposta", loginUser.EPosta);
        //            cmd.Parameters.AddWithValue("@parola", loginUser.Parola);

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    // Kullanıcı bilgilerini session'a kaydet (isteğe bağlı)
        //                    HttpContext.Session.SetString("KullaniciAdi", reader["KullaniciAdi"].ToString());
        //                    HttpContext.Session.SetString("EPosta", reader["EPosta"].ToString());

        //                    // Giriş başarılı, Index sayfasına yönlendir
        //                    return RedirectToAction("Index", "Home");
        //                }
        //                else
        //                {
        //                    ViewBag.Error = "Geçersiz e-posta veya parola.";
        //                    return View("Login");
        //                }
        //            }
        //        }
        //    }
        //}

        [HttpPost]
        public IActionResult LoginAjax(User loginUser) 
        {
            try
            {
                if (string.IsNullOrEmpty(loginUser.EPosta) || string.IsNullOrEmpty(loginUser.Parola))
                {
                    return Json(new { success = false, message = "Lütfen e-posta ve parola alanlarını doldurun." });
                }

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();
                    string query = @"SELECT Id, KullaniciAdi, EPosta, Role FROM [User] 
                    WHERE EPosta = @eposta AND Parola = @parola AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@eposta", loginUser.EPosta);
                        cmd.Parameters.AddWithValue("@parola", loginUser.Parola);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                HttpContext.Session.SetString("UserId", reader["Id"].ToString());
                                HttpContext.Session.SetString("UserName", reader["KullaniciAdi"].ToString());
                                HttpContext.Session.SetString("UserEmail", reader["EPosta"].ToString());
                                HttpContext.Session.SetString("UserRole", reader["Role"].ToString());

                                return Json(new { success = true, message = "Giriş başarılı! Yönlendiriliyorsunuz..." });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Geçersiz e-posta veya parola." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }


        public IActionResult Signin()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View("Signin", user); 
            }

            using (SqlConnection conn = new SqlConnection(connectionStr))
            {
                conn.Open();
                string query = "INSERT INTO [User] (KullaniciAdi, EPosta, Parola) VALUES (@username, @email, @password)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", user.KullaniciAdi);
                    cmd.Parameters.AddWithValue("@email", user.EPosta);
                    cmd.Parameters.AddWithValue("@password", user.Parola); 

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult UserOperations()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetUsersPaged(int page = 1, int pageSize = 10) 
        {
            var users = new List<User>();
            int totalCount = 0;
            using (var connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM [dbo].[User] WHERE IsActive = 1", connection))
                {
                    totalCount = (int)countCmd.ExecuteScalar();
                }
                string sql = @"
    SELECT Id, KullaniciAdi, EPosta, Parola, IsActive, Role 
    FROM [dbo].[User] 
    WHERE IsActive = 1
    ORDER BY Id
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                KullaniciAdi = reader.GetString(1),
                                EPosta = reader.GetString(2),
                                Parola = reader.GetString(3),
                                IsActive = reader.GetBoolean(4),
                                Role = reader.IsDBNull(5) ? "Admin" : reader.GetString(5) 
                            });
                        }
                    }
                }
            }
            var result = new
            {
                totalCount = totalCount, 
                users = users  
            };
            return Json(result);
        }

        [HttpPost]
        public JsonResult SoftDeleteUser(int id) 
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Id = @Id AND IsActive = 1";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        int existingCount = (int)checkCmd.ExecuteScalar();

                        if (existingCount == 0)
                        {
                            return Json(new { success = false, message = "Aktif kullanıcı bulunamadı." });
                        }
                    }

                    string updateQuery = "UPDATE [User] SET IsActive = 0 WHERE Id = @Id";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@Id", id);
                        int affectedRows = updateCmd.ExecuteNonQuery();

                        if (affectedRows > 0)
                        {
                            return Json(new { success = true, message = "Kullanıcı başarıyla silindi." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Silme işlemi başarısız." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegisterUserAjax([FromBody] User user) 
        {
            try
            {
                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "admin";
                }

                user.IsActive = true; 

                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string query = @"
                INSERT INTO [User] (KullaniciAdi, EPosta, Parola, IsActive, Role)
                OUTPUT INSERTED.Id
                VALUES (@KullaniciAdi, @EPosta, @Parola, @IsActive, @Role)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KullaniciAdi", user.KullaniciAdi);
                        cmd.Parameters.AddWithValue("@EPosta", user.EPosta);
                        cmd.Parameters.AddWithValue("@Parola", user.Parola);
                        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                        cmd.Parameters.AddWithValue("@Role", user.Role);

                        int newId = (int)cmd.ExecuteScalar();

                        return Json(new
                        {
                            success = true,
                            data = new
                            {
                                Id = newId,
                                KullaniciAdi = user.KullaniciAdi,
                                EPosta = user.EPosta,
                                Role = user.Role
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public ActionResult Harita()
        {
            return View();
        }

        public IActionResult RoleOperations()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UpdateUserRole(int id, string role) 
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionStr))
                {
                    conn.Open();

                    string query = "UPDATE [User] SET Role = @Role WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}