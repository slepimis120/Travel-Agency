﻿using HCI_Tim_15_2023.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace HCI_Tim_15_2023.GUI
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private string GenerateUniqueID(IMongoCollection<User> collection)
        {
            string newId;
            bool idExists;

            do
            {
                int randomNumber = new Random().Next(100000, 999999);
                newId = randomNumber.ToString();

                var idFilter = Builders<User>.Filter.Eq(r => r.id, newId);
                idExists = collection.Find(idFilter).Any();
            } while (idExists);

            return newId;
        }


        private List<User> GetUsersFromDB()
        {
            string connectionString = "mongodb://localhost:27017";
            string databaseName = "hci";
            string collectionName = "users";

            var client = new MongoClient(connectionString);

            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<User>(collectionName);
            var filter = Builders<User>.Filter.Empty;
            var users = collection.Find(filter).ToList();

            return users;
        }

        private void CreateAccount(object sender, RoutedEventArgs e)
        {
            string connectionString = "mongodb://localhost:27017";
            string databaseName = "hci";
            string collectionName = "users";

            var client = new MongoClient(connectionString);

            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<User>(collectionName);
            int cost;

            User newUser = new User(GenerateUniqueID(collection), Username.Text, Password.Text, roles.CLIENT);

            collection.InsertOne(newUser);
            MessageBox.Show("Account created successfully!");

            this.NavigationService.Navigate(new LogInPage());
        }
    }
}
