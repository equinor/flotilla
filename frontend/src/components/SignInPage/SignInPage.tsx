import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { SignInButton } from "./SignInButton";

export const SignInPage = () => {
    const isAuthenticated = useIsAuthenticated();
    
    return (
        <>
            <div className = "sign-in-button">
                { isAuthenticated ? <span>Signed In</span> : <SignInButton/> }
            </div>
        </>
    )
}
