import { AccountInfo, PublicClientApplication } from "@azure/msal-browser";
import { AccountIdentifiers, IMsalContext, useMsal } from "@azure/msal-react";
import { useState } from "react";

export const msalConfig = {
    auth: {
        clientId: "f5993820-b7e2-4791-886f-f9f5027dc7be",
        authority: "https://login.microsoftonline.com/3aa4a235-b6e2-48d5-9195-7fcf05b459b0",
        redirectUri: "http://localhost:3001"
    },
    cache: {
        cacheLocation: "sessionStorage"
    }
};

export const loginRequest = {
    scopes: ["api://ea4c7b92-47b3-45fb-bd25-a8070f0c495c/user_impersonation"]
}

export const APIConfig = {
    backendAPIEndpoint: "https://localhost:8000",
    originList: "http://localhost:3001"
};

export async function fetchAccessToken(context: IMsalContext):Promise<string> {

        const request = {
            ...loginRequest,
            account: context.accounts[0]
        };
        
        // Silently acquires an access token which is then attached to a request for Microsoft Graph data
        return context.instance.acquireTokenSilent(request).then((response) => {
            const accessToken:string = response.accessToken ?? ""
            return accessToken
        })
        .catch(() => {
            return context.instance.acquireTokenPopup(request).then((response) => {
                return response.accessToken
            });
        });
    }
    