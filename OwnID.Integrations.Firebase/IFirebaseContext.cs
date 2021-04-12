using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;

namespace OwnID.Integrations.Firebase
{
    public interface IFirebaseContext
    {
        FirebaseAuth Auth { get; init; }
        
        FirebaseApp App { get; init; }
        
        FirestoreDb Db { get; init; }
    }
}