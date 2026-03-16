using System;

namespace ValorantRentalClient
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ValorantKey
    {
        public int KeyId { get; set; }
        public string KeyCode { get; set; }
        public string PackageType { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsSold { get; set; }
        public string Status { get; set; }
    }

    public class RiotAccount
    {
        public int AccountId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Region { get; set; }
        public bool IsAvailable { get; set; }
        public string AccountStatus { get; set; }
    }

    public class Session
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int KeyId { get; set; }
        public int AccountId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
    }

    public class Transaction
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int KeyId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}