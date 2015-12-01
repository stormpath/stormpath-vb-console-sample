﻿' <copyright file="DemoSync.vb" company="Stormpath, Inc.">
' Copyright (c) 2015 Stormpath, Inc.
'
' Licensed under the Apache License, Version 2.0 (the "License");
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at
'
'      http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.
' </copyright>

Option Strict On
Option Explicit On
Option Infer On
Imports Stormpath.SDK
Imports Stormpath.SDK.Account
Imports Stormpath.SDK.Api
Imports Stormpath.SDK.Client
Imports Stormpath.SDK.Error
Imports Stormpath.SDK.Group
Imports Stormpath.SDK.Sync

' This demo is identical in behavior to Demo.vb
' but uses the Stormpath.SDK.Sync namespace methods to
' access the Stormpath API synchronously.
' This isn't just a sync-over-async wrapper - it's
' full dual-stack support in the SDK.

Public Class DemoSync
    Public Sub RunDemo()
        Console.WriteLine("Running demo synchronously...")

        ' Load an API Key and Secret from the specified file path
        ' This is only necessary if the API Key is not stored in environment variables
        ' or in the default location (~\.stormpath\apiKey.properties).
        Dim apiKey = ClientApiKeys.Builder _
            .SetFileLocation("~\.stormpath\apiKey.properties") _
            .Build()

        ' Build a client object - everything starts here!
        ' .SetApiKey() is only necessary if specifying an API Key location above.
        Dim client = Clients.Builder _
            .SetApiKey(apiKey) _
            .Build()

        ' Get the default "My Application" application
        Dim app = client.GetApplications _
            .Synchronously() _
            .Where(Function(a) a.Name = "My Application") _
            .First()
        Console.WriteLine("Connected to Stormpath")

        ' Create a user who can log into the application
        Dim joe = client.Instantiate(Of IAccount) _
            .SetGivenName("Joe") _
            .SetSurname("Stormtrooper") _
            .SetEmail("tk421@deathstar.co") _
            .SetPassword("Changeme!123")

        app.CreateAccount(joe)
        Console.WriteLine("Created account " & joe.Email)

        ' Try logging in Joe
        Try
            Dim loginResult = app.AuthenticateAccount("tk421@deathstar.co", "Changeme!123")
            Dim loginAccount = loginResult.GetAccount()
            Console.WriteLine("User " & loginAccount.FullName & " logged in!")
        Catch rex As ResourceException
            Console.WriteLine("Could not log in. Error: " & rex.Message)
        End Try

        ' Create a demo group for Joe to be part of
        ' And an admin group Joe is NOT part of
        ' (In a production application, these would be created beforehand and only once)
        Dim demoUsers = client.Instantiate(Of IGroup) _
            .SetName("DemoUsers") _
            .SetDescription("Demo users who do not have administrator access.")
        Dim demoAdmins = client.Instantiate(Of IGroup) _
            .SetName("DemoAdmins") _
            .SetDescription("Demo users who have administrator access.")

        app.CreateGroup(demoUsers)
        app.CreateGroup(demoAdmins)

        ' Add Joe to the Users group
        joe.AddGroup(demoUsers)

        ' Get role-based authorization from group
        Dim roleNames = joe.GetGroups _
            .Synchronously() _
            .ToList() _
            .Select(Function(g) g.Name)
        Console.WriteLine("Roles for " & joe.GivenName & ": " &
                          String.Join(", ", roleNames))

        ' Save fine-grained permissions to Joe's account using custom data
        joe.CustomData.Put(New With {.read = True, .write = False})
        joe.Save()

        ' Get fine-grained permissions from custom data
        Dim joeCustomData = joe.GetCustomData()
        Dim canRead = CBool(joeCustomData("read"))
        Dim canWrite = CBool(joeCustomData("write"))
        Console.WriteLine("Can Joe read? " & canRead)
        Console.WriteLine("Can Joe write? " & canWrite)

        ' Reset Joe's password. This initiates a password reset workflow:
        ' An email is sent to Joe, which includes a callback link to your
        ' application, and a token in the URL queryString.
        Dim token = app.SendPasswordResetEmail("tk421@deathstar.co")
        ' In the controller that handles the callback action, capture the token from the queryString.
        ' Once you have the token, the workflow can be completed.
        app.ResetPassword(token.GetValue(), "ItsATrap1138!")
        Console.WriteLine("Password reset for " & joe.Email)

        ' Clean up
        demoAdmins.Delete()
        demoUsers.Delete()
        joe.Delete()
        Console.WriteLine("Cleaned up API objects")

        ' Wait for user input before closing console window
        Console.WriteLine("Done!")
        Console.ReadKey(False)
    End Sub
End Class
