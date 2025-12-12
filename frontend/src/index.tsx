import App from './App'
import { StrictMode } from 'react'
import { PublicClientApplication, EventType } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { msalConfig } from 'api/AuthConfig'
import ReactDOM from 'react-dom/client'

// ========================================

const msalInstance = new PublicClientApplication(msalConfig)

// Error handling for redirect login
await msalInstance.initialize()

// Add event callback to prevent multiple interactions
msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS) {
        console.log('Authentication successful')
    }
})

msalInstance
    .handleRedirectPromise()
    .then((tokenResponse) => {
        if (tokenResponse) {
            console.log('User authenticated successfully')
        }
    })
    .catch((err) => {
        console.error('Authentication error:', err)
    })

const rootElement = document.getElementById('root')
if (!rootElement) throw new Error('Failed to find the root element')
const root = ReactDOM.createRoot(rootElement)

root.render(
    <StrictMode>
        <MsalProvider instance={msalInstance}>
            <App />
        </MsalProvider>
    </StrictMode>
)
