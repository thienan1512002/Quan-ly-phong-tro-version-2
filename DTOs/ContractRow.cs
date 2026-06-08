namespace QuanLyPhongTro.DTOs
{
    public class ContractRow
    {
        public int id { get; set; }
        public string roomName { get; set; } = "";
        public string roomAddress { get; set; } = "";
        public double roomArea { get; set; }
        public double roomPrice { get; set; }
        public string roomPriceFormatted { get; set; } = "";
        public string roomDesc { get; set; } = "";
        public string roomStatus { get; set; } = "";
        public string userName { get; set; } = "";
        public string userEmail { get; set; } = "";
        public string userUsername { get; set; } = "";
        public string userPhone { get; set; } = "";
        public string startDate { get; set; } = "";
        public string endDate { get; set; } = "";
        public int durationDays { get; set; }
        public double monthlyRent { get; set; }
        public string monthlyRentFormatted { get; set; } = "";
        public string note { get; set; } = "";
        public bool isActive { get; set; }
        public string statusText { get; set; } = "";
        public string createdAt { get; set; } = "";
    }
}
