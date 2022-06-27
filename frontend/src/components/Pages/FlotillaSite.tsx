import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { createContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { FrontPage } from './FrontPage'
import { MissionPage } from './MissionPage'

export const AccessTokenContext = createContext('')

export function FlotillaSite() {
    const authContext = useMsal()
    const [accessToken, setAccessToken] = useState('')
    useEffect(() => {
        fetchAccessToken(authContext).then((accessToken) => {
            setAccessToken(accessToken)
        })
    }, [])
    return (
        <>
            {accessToken === '' && <>Loading...</>}
            {accessToken !== '' && (
                <>
                    <AccessTokenContext.Provider value={accessToken}>
                        <BrowserRouter>
                            <Routes>
                                <Route path="/" element={<FrontPage />} />
                                <Route path="/mission" element={<MissionPage />} />
                            </Routes>
                        </BrowserRouter>
                    </AccessTokenContext.Provider>
                </>
            )}
        </>
    )
}
