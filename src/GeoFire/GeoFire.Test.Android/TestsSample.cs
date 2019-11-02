using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.CloudFirestore;
using Xunit;

namespace GeoFire.Test.Android
{
    public class TestsSample
    {

        class Test
        {
            public string Name { get; set; }
        }
        
        [Fact]
        public async Task GetAndSetLocation()
        {
            var geoFire = new GeoFire("test");
            try
            {
                await CrossCloudFirestore.Current.Instance
                    .GetCollection("test")
                    .GetDocument("ququ2")
                    .SetDataAsync(new Dictionary<string, object>
                    {
                        {"test", "test"}
                    });
                await geoFire.SetLocationAsync("ququ2", new GeoPoint(5, 5));
                var geoPoint = await geoFire.GetLocationAsync("ququ2");
                Assert.Equal(new GeoPoint(5, 5), geoPoint);
            }
            catch (Exception e)
            {
                Assert.Null(e);
            }
            Assert.True(true);
        }
        
        [Fact]
        public async Task RemoveLocation()
        {
            var geoFire = new GeoFire("test");
            try
            {
                await CrossCloudFirestore.Current.Instance
                    .GetCollection("test")
                    .GetDocument("ququ2")
                    .SetDataAsync(new Dictionary<string, object>
                    {
                        {"test", "test"}
                    });
                await geoFire.SetLocationAsync("ququ2", new GeoPoint(5, 5));
                await geoFire.RemoveLocationAsync("ququ2");
                var doc = await CrossCloudFirestore.Current.Instance.GetDocument("/test/ququ2").GetDocumentAsync();
                Assert.True(doc.Exists);
                Assert.True(!doc.Data.ContainsKey("q"));
                Assert.True(!doc.Data.ContainsKey("lt"));
                Assert.True(!doc.Data.ContainsKey("gt"));
                Assert.True(doc.Data.ContainsKey("test"));
            }
            catch (Exception e)
            {
                Assert.Null(e);
            }
            Assert.True(true);
        }

        [Fact]
        public async Task TestQuery()
        {
            var geoFire = new GeoFire("test");
            try
            {
                var doc = CrossCloudFirestore.Current.Instance
                    .GetCollection("test")
                    .CreateDocument();
                await doc.SetDataAsync(new Test { Name = "Sofia" });
                await geoFire.SetLocationAsync(doc.Id, new GeoPoint(5, 5));
                
                var query = geoFire.QueryAtLocation<Test>(new GeoPoint(5, 5), 10);
                query.OnDocumentEntered += (sender, args) =>
                {
                    Assert.IsType<Test>(args.Document);
                };
            }
            catch (Exception e)
            {
                Assert.Null(e);
            }
            Assert.True(true);
        }
    }
}
