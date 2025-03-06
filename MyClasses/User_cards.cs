using System;
using System.ComponentModel.DataAnnotations;

namespace MyClasses
{
    public class User_cards
    {
        
        public int card_id { get; set; }

    
        public int user_id { get; set; }

       
        public string first_name { get; set; }

     
        public string username { get; set; }

        public string bio { get; set; }

       
        public string games { get; set; } // Stores game preferences


        public DateTime created_at { get; set; }

        
        public string picture_url { get; set; } // Profile picture

        // Constructor
        public User_cards() { }

        public User_cards(int card_id, int user_id, string first_name, string username, string bio, string games, DateTime created_at, string picture_url)
        {
            this.card_id = card_id;
            this.user_id = user_id;
            this.first_name = first_name;
            this.username = username;
            this.bio = bio;
            this.games = games;
            this.created_at = created_at;
            this.picture_url = picture_url;
        }
    }
}
