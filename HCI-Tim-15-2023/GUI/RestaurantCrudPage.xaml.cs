﻿using HCI_Tim_15_2023.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongoDB.Bson;

namespace HCI_Tim_15_2023.GUI;
public partial class RestaurantCrudPage : Page
{
    public Restaurant selectedRestaurant { get; set; }

    public bool IsReadOnly { get; set; }
    public RestaurantCrudPage()
    {
        InitializeComponent();
        IsReadOnly = true;
        var restaurants = GetRestaurantsFromDB();
        restaurantsDataGrid.ItemsSource = restaurants;
        selectedRestaurant = new Restaurant("123", 0.0, 0.0, "Sample Address", "Sample Name", 10);
        DataContext = this;
    }
    private void RestaurantsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsReadOnly = true;
        selectedRestaurant = (Restaurant)restaurantsDataGrid.SelectedItem;
    
        DataContext = null;
        DataContext = this;
    }

    private List<Restaurant> GetFilteredRestaurantsFromDB(string searchTerm)
    {
        string connectionString = "mongodb://localhost:27017";
        string databaseName = "hci";
        string collectionName = "restaurants";

        var client = new MongoClient(connectionString);

        var database = client.GetDatabase(databaseName);
        var collection = database.GetCollection<Restaurant>(collectionName);

        var filterBuilder = Builders<Restaurant>.Filter;
        var filter = filterBuilder.Or(
            filterBuilder.Regex(r => r.name, new BsonRegularExpression(searchTerm, "i")),
            filterBuilder.Regex(r => r.address, new BsonRegularExpression(searchTerm, "i"))
        );

        var filteredRestaurants = collection.Find(filter).ToList();

        return filteredRestaurants;
    }
    private List<Restaurant> GetRestaurantsFromDB()
    {
        string connectionString = "mongodb://localhost:27017";
        string databaseName = "hci";
        string collectionName = "restaurants";

        var client = new MongoClient(connectionString);

        var database = client.GetDatabase(databaseName);
        var collection = database.GetCollection<Restaurant>(collectionName);
        var filter = Builders<Restaurant>.Filter.Empty;
        var restaurants = collection.Find(filter).ToList();

        return restaurants;
    }
    private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        if (textBox.Text == "Search")
        {
            textBox.Text = string.Empty;
            textBox.Foreground = Brushes.Black;
            textBox.Opacity = 1.0;
        }
    }
    
    private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            textBox.Text = "Search";
            textBox.Foreground = Brushes.Gray;
            textBox.Opacity = 0.5;
        }
    }
    private async Task SetCoordinatesFromAddress(Restaurant restaurant)
    {
        string apiUrl = "https://nominatim.openstreetmap.org/search?format=json&street=" +
                        Uri.EscapeDataString(restaurant.address) + "+&city=" + "Belgrade";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Invalid address.");
            }
        }
    }


    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        RestaurantDialog dialog = new RestaurantDialog();

        bool? result = dialog.ShowDialog();

        if (result == true)
        {
            string connectionString = "mongodb://localhost:27017";
            string databaseName = "hci";
            string collectionName = "restaurants";

            var client = new MongoClient(connectionString);

            var database = client.GetDatabase(databaseName);
            var collection = database.GetCollection<Restaurant>(collectionName);
            string name = dialog.NameTextBox.Text;
            string address = dialog.AddressTextBox.Text;
            
            int cost;
            if (!int.TryParse(dialog.CostTextBox.Text, out cost))
            {
                MessageBox.Show("Invalid cost value. Please enter a valid integer.");
                return;
            }

            Restaurant newRestaurant = new Restaurant
            {
                name = name,
                address = address,
                cost = cost,
                id = GenerateUniqueID(collection)
            };
            SetCoordinatesFromAddress(newRestaurant);

            collection.InsertOne(newRestaurant);
            var restaurants = GetRestaurantsFromDB();
            restaurantsDataGrid.ItemsSource = restaurants;
            MessageBox.Show("Restaurant added successfully!");
        }
    }


    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string searchTerm = searchTextBox.Text;

        var filteredRestaurants = GetFilteredRestaurantsFromDB(searchTerm);
        if (filteredRestaurants.Count != 0)
        {
            restaurantsDataGrid.ItemsSource = filteredRestaurants;
        }
        
    }private string GenerateUniqueID(IMongoCollection<Restaurant> collection)
    {
        string newId;
        bool idExists;

        do
        {
            int randomNumber = new Random().Next(100000, 999999);
            newId = randomNumber.ToString();

            var idFilter = Builders<Restaurant>.Filter.Eq(r => r.id, newId);
            idExists = collection.Find(idFilter).Any();
        } while (idExists);

        return newId;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly)
        {
            editButton.Content = "Cancel";
            IsReadOnly = false;
            DataContext = null;
            DataContext = this;
            confirmButton.IsEnabled = true;
        }else
        {
            editButton.Content = "Edit";
            IsReadOnly = true;
            DataContext = null;
            DataContext = this;
            confirmButton.IsEnabled = false;
        }

    }

    private void ConfirmButton_OnClickButton_Click(object sender, RoutedEventArgs e)
    {
        string newName = nameTextBox.Text;
        string newAddress = addressTextBox.Text;
        int newCost;
        string restaurantId = selectedRestaurant.id;
        if (!int.TryParse(costTextBox.Text, out newCost))
        {
            MessageBox.Show("Invalid cost value.");
            return;
        }
        if (newName.Length == 0 || newAddress.Length == 0 || newCost <= 0)
        {
            MessageBox.Show("Input fields can't be empty!");
            return;
        }

        FilterDefinition<Restaurant> filter = Builders<Restaurant>.Filter.Eq(r => r.id, restaurantId);


        UpdateDefinition<Restaurant> update = Builders<Restaurant>.Update
            .Set(r => r.name, newName)
            .Set(r => r.address, newAddress)
            .Set(r => r.cost, newCost);

        string connectionString = "mongodb://localhost:27017";
        string databaseName = "hci";
        string collectionName = "restaurants";

        var client = new MongoClient(connectionString);

        var database = client.GetDatabase(databaseName);
        var collection = database.GetCollection<Restaurant>(collectionName);
        var result = collection.UpdateOne(filter, update);

        if (result.ModifiedCount > 0)
        {
            // Update was successful
            MessageBox.Show("Restaurant updated successfully!");
        }
        else
        {
            // Update did not find a matching document
            MessageBox.Show("No restaurant found with the given ID.");
        }
    }

}
