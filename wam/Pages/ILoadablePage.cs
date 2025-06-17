// Dosya: ILoadablePage.cs
using System.Threading.Tasks;

namespace wam.Pages
{
    public interface ILoadablePage
    {
        Task LoadDataAsync();
        
        /// <summary>
        /// Sayfa verilerini JSON formatında export eder
        /// </summary>
        void ExportToJson();
        
        /// <summary>
        /// Sayfa verilerini CSV formatında export eder
        /// </summary>
        void ExportToCsv();
        
        /// <summary>
        /// Sayfa verilerini otomatik export eder (kullanıcı seçimi ile)
        /// </summary>
        void AutoExport();
        
        /// <summary>
        /// Modül adını döndürür (export dosya adı için)
        /// </summary>
        string GetModuleName();
    }
}