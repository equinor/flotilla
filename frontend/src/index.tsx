import App from './App'
import { StrictMode } from 'react'
import { PublicClientApplication, EventType, AccountInfo } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from 'api/AuthConfig'
import ReactDOM from 'react-dom/client'

// ========================================

const msalInstance = new PublicClientApplication(msalConfig)

await msalInstance.initialize()

// Handle redirect BEFORE rendering
const redirectResult = await msalInstance.handleRedirectPromise().catch((err) => {
    console.error('Authentication error:', err)
    return null
})

// If we got a redirect result, set that account active
if (redirectResult?.account) {
    msalInstance.setActiveAccount(redirectResult.account)
} else {
    // Otherwise, if already signed in from cache, set the first account active
    const accounts = msalInstance.getAllAccounts()
    if (accounts.length > 0) {
        msalInstance.setActiveAccount(accounts[0] as AccountInfo)
    }
}

msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
        const account = (event.payload as any).account
        if (account) msalInstance.setActiveAccount(account)
    }
})

const rootElement = document.getElementById('root')
if (!rootElement) throw new Error('Failed to find the root element')

ReactDOM.createRoot(rootElement).render(
    <StrictMode>
        <MsalProvider instance={msalInstance}>
            <App />
        </MsalProvider>
    </StrictMode>
)
