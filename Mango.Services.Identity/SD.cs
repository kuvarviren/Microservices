using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Mango.Services.Identity
{
    public static class SD
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
        //Configure Identity Server
        /*
         *  Identity Resource:  identity resource is a named group of claims that can be requested using a scope parameter.
         *  So resources will be basically something that you want to protect with your identity server, either identity data 
         *  of your users or the API itself.
         *  Identity resources are data like user Id, name or email address of a user.
         *  So what will happen is an identity resource has a unique name, you can assign the arbitrary claim types to that.
         *  So basically names will be stored in some claim types and then when you access the identity token, all of these 
         *  identity resources will be provided in some type of claim.         
         */

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),//Initializes the OpenId
                new IdentityResources.Email(),
                new IdentityResources.Profile(),
            };
        /*
         * Scopes: scopes are basically identifiers for resource that the client wants to access.
         * In our case, the client will be Mango.Web project because that will be using identity servers to retrieve tokens.
         * There are two types of scopes in identity server, one is identity scope, and the next one is resource scope.
         * Identity scope will have an object of the profile itself. One example is the profile will have an identity scope and 
         * that will include first name, last name,user name and so on.
         */
        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope> {
                new ApiScope("mango", "Mango Server"),
                new ApiScope(name: "read",   displayName: "Read your data."),
                new ApiScope(name: "write",  displayName: "Write your data."),
                new ApiScope(name: "delete", displayName: "Delete your data.")
            };
        /*
         *  Client: Scope is used by Clients. And client can perform the opeation based on Scope assigned to it.
         *  client is a piece of software that request a token from identity server. It can be either for authenticating a user 
         *  or for accessing a resource, for example, clients are web application, mobile application, desktop applications, etc..         
         */

        public static IEnumerable<Client> Clients =>
          new List<Client>
          {
                new Client
                {
                    ClientId="client",
                    ClientSecrets= { new Secret("secret".Sha256())},
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes={ "read", "write","profile"}
                },
                new Client
                {
                    ClientId="mango",
                    ClientSecrets= { new Secret("secret".Sha256())},
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris={ "https://localhost:7054/signin-oidc" },
                    PostLogoutRedirectUris={"https://localhost:7054/signout-callback-oidc" },
                    AllowedScopes=new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "mango"
                    }
                },
          };
    }
}
