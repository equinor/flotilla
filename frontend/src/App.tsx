import { Icon, Typography } from '@equinor/eds-core-react'
import { platform } from '@equinor/eds-icons'
import { RobotOverview } from './components/RobotOverview'
import { defaultRobots } from './models/robot'
import './app.css'
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { SignInPage } from './components/SignInPage/SignInPage'
import { ProfileContent } from 'components/SignInPage/ProfileContent'
import { createContext, useEffect, useState } from 'react'
import { fetchAccessToken } from 'authConfig'
Icon.add({ platform })

const robots = [defaultRobots['taurob'], defaultRobots['exRobotics'], defaultRobots['turtle']]

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
                    <ProfileContent />
                    <div className="header">
                        <Typography color="primary" variant="h1" bold>
                            Flotilla
                        </Typography>
                    </div>
                    <div className="robot-overview">
                        <RobotOverview robots={robots}></RobotOverview>
                    </div>
                    <div className="mission-overview">
                        <Typography variant="h2" style={{ marginTop: '20px' }}>
                            Mission Overview
                        </Typography>
                    </div>
                </AuthenticatedTemplate>
            </div>
        </AccessTokenContext.Provider>
    )
}

export default App
