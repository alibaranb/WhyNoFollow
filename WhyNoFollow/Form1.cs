using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WhyNoFollow
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Attach the DoubleClick event to the ListBox
            listBox1.DoubleClick += ListBox1_DoubleClick;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Load JSON files
                string followersFilePath = "followers_1.json";
                string followingFilePath = "following.json";

                // Parse the JSON data
                JArray followersArray = ParseJsonArrayFromFile(followersFilePath);
                JArray followingArray = ParseNestedJsonArray(followingFilePath, "relationships_following");

                // Extract values and URLs from both files
                Dictionary<string, string> followers = ExtractValuesAndUrls(followersArray);
                Dictionary<string, string> following = ExtractValuesAndUrls(followingArray);

                // Find values in "following" that are not in "followers"
                var notFollowingBack = following
                    .Where(pair => !followers.ContainsKey(pair.Key))
                    .ToList();

                // Clear the ListBox before adding new items
                listBox1.Items.Clear();

                // Add values and URLs to ListBox
                foreach (var pair in notFollowingBack)
                {
                    // Add the username and attach the URL as the Tag
                    listBox1.Items.Add(new ListBoxItemWithLink(pair.Key, pair.Value));
                }

                // Show message if no unmatched values found
                if (!notFollowingBack.Any())
                {
                    MessageBox.Show("Everyone you follow is following you back!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            // Ensure an item is selected
            if (listBox1.SelectedItem is ListBoxItemWithLink selectedItem)
            {
                string url = selectedItem.Link;
                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        // Open the URL in the default browser
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true // Required for modern browsers
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open the link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private JArray ParseJsonArrayFromFile(string filePath)
        {
            string jsonData = File.ReadAllText(filePath);
            try
            {
                return JArray.Parse(jsonData);
            }
            catch
            {
                throw new InvalidOperationException($"The file {filePath} does not contain a valid JSON array.");
            }
        }

        private JArray ParseNestedJsonArray(string filePath, string rootProperty)
        {
            string jsonData = File.ReadAllText(filePath);
            try
            {
                JObject rootObject = JObject.Parse(jsonData);
                JToken nestedArray = rootObject[rootProperty];
                if (nestedArray is JArray array)
                {
                    return array;
                }
                else
                {
                    throw new InvalidOperationException($"The property '{rootProperty}' in file {filePath} is not a valid JSON array.");
                }
            }
            catch
            {
                throw new InvalidOperationException($"The file {filePath} does not contain a valid JSON object with a property '{rootProperty}'.");
            }
        }

        private Dictionary<string, string> ExtractValuesAndUrls(JArray jsonArray)
        {
            Dictionary<string, string> valuesAndUrls = new Dictionary<string, string>();

            foreach (var obj in jsonArray)
            {
                var stringListData = obj["string_list_data"];
                if (stringListData != null)
                {
                    foreach (var item in stringListData)
                    {
                        string value = item["value"]?.ToString();
                        string href = item["href"]?.ToString();

                        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(href))
                        {
                            valuesAndUrls[value] = href;
                        }
                    }
                }
            }

            return valuesAndUrls;
        }
    }

    // Custom ListBox item class to store the link
    public class ListBoxItemWithLink
    {
        public string Username { get; }
        public string Link { get; }

        public ListBoxItemWithLink(string username, string link)
        {
            Username = username;
            Link = link;
        }

        public override string ToString()
        {
            return Username; // Display only the username in the ListBox
        }
    }
}
