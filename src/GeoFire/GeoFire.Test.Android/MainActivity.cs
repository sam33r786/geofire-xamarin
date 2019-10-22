using System.Reflection;

using Android.App;
using Android.OS;
using Firebase;
using Xamarin.Android.NUnitLite;

namespace GeoFire.Test.Android
{
    [Activity(Label = "GeoFire.Test.Android", MainLauncher = true)]
    public class MainActivity : TestSuiteActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            FirebaseApp.InitializeApp(this);
            
            // tests can be inside the main assembly
            AddTest(Assembly.GetExecutingAssembly());
            // or in any reference assemblies
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            
            base.OnCreate(bundle);
            
        }
    }
}
