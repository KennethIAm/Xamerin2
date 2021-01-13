using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace GoFishClient
{
    public class User
    {
        private byte point;
        public byte Point
        {
            get { return point; }
            set { point = value; }
        }

        private byte requestType;
        public byte RequestType
        {
            get { return requestType; }
            set { requestType = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
        }

        private string email;
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private int wins;
        public int Wins
        {
            get { return wins; }
            set { wins = value; }
        }

        private string ip;
        public string IP
        {
            get { return ip; }
        }

        public User(string name, string email, int wins, string ip)
        {
            this.name = name;
            Email = email;
            Wins = wins;
            this.ip = ip;
        }
    }
}