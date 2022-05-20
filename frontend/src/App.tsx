import './app.css'
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { SignInPage } from './components/SignInPage/SignInPage'
import { createContext, useEffect, useState } from 'react'
import { fetchAccessToken } from 'authConfig'
import { BrowserRouter as Router, Routes, Route, Link, BrowserRouter } from 'react-router-dom'
import { FrontPage } from 'components/FrontPage/FrontPage'
import { TestPage } from 'components/FrontPage/TestPage'

// Icon.add({ platform })

// const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

export const AccessTokenContext = createContext('')

function App() {
    const authContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    useEffect(() => {
        fetchAccessToken(authContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }, [])
    return (
        <AccessTokenContext.Provider value={accessToken}>
            <div className="app-ui">
                <UnauthenticatedTemplate>
                    <div className="sign-in-page">
                        <SignInPage></SignInPage>
                    </div>
                </UnauthenticatedTemplate>
                <AuthenticatedTemplate>
                    <BrowserRouter>
                        <Routes>
                            <Route path="/" element={<FrontPage />} />
                            <Route path="test" element={<TestPage />} />
                        </Routes>
                    </BrowserRouter>
                </AuthenticatedTemplate>
            </div>
        </AccessTokenContext.Provider>
    )
}

export default App
