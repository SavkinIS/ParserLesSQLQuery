using System;
using System.Collections.Generic;

namespace ParserLes.Model
{
    public class Deal
    {
        public Deal(string dealNumber, string sellerName, string sellerInn, string buyerName, string buyerInn, string dealDate, float? woodVolumeBuyer, float? woodVolumeSeller)
        {
            DealNumber = dealNumber;
            SellerName = sellerName != null ? sellerName : "";
            SellerInn = sellerInn != null ? sellerInn : "";
            BuyerName = buyerName != null ? buyerName : "";
            BuyerInn = buyerInn != null ? buyerInn : "";
            DealDate = Convert.ToDateTime(dealDate);
            WoodVolumeBuyer = woodVolumeBuyer;
            WoodVolumeSeller = woodVolumeSeller;
        }

        public string DealNumber { get; set; }
        public string SellerName { get; set; }
        public string SellerInn { get; set; }
        public string BuyerName { get; set; }
        public string BuyerInn { get; set; }
        public DateTime? DealDate { get; set; }
        public float? WoodVolumeBuyer { get; set; }
        public float? WoodVolumeSeller { get; set; }


        public string DealToString()
        {
            return $"N'{DealNumber}', N'{SellerName.Replace('\'', '`')}', N'{SellerInn}', N'{BuyerName.Replace('\'', '`')}', N'{BuyerInn}', {DealDate.Value.Date.ToString("yyyy-MM-dd")}, '{WoodVolumeSeller.Value.ToString().Replace(',','.')}', '{WoodVolumeBuyer.Value.ToString().Replace(',', '.')}'";
        }
    }


    class AllDeals
    {
        public List<Deal> content { get; set; }
    }

    class JSONData
    {
        public int ID { get; set; }
        public string Data { get; set; }
    }
}
