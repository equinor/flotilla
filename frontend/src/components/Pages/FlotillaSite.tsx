import { config } from 'config'
import { useMsal } from '@azure/msal-react'
import { fetchAccessToken } from 'api/AuthConfig'
import { Header } from 'components/Header/Header'
import { createContext, useEffect, useState } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import styled from 'styled-components'
import { FrontPage } from './FrontPage/FrontPage'
import { MissionPage } from './MissionPage/MissionPage'
import { AssetProvider } from 'components/Contexts/AssetContext'
import { LanguageProvider } from 'components/Contexts/LanguageContext'
import { HistoricPage } from './HistoricPage/HistoricPage'

export const AccessTokenContext = createContext('')

const StyledPages = styled.div`
    margin: 2rem;
`

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
                    <LanguageProvider>
                        <AssetProvider>
                            <AccessTokenContext.Provider value={accessToken}>
                                <Header />
                                <StyledPages>
                                    <BrowserRouter>
                                        <Routes>
                                            <Route path={`${config.FRONTEND_BASE_ROUTE}/`} element={<FrontPage />} />
                                            <Route
                                                path={`${config.FRONTEND_BASE_ROUTE}/mission/:missionId`}
                                                element={<MissionPage />}
                                            />
                                            <Route
                                                path={`${config.FRONTEND_BASE_ROUTE}/historic`}
                                                element={<HistoricPage />}
                                            />
                                        </Routes>
                                    </BrowserRouter>
                                </StyledPages>
                            </AccessTokenContext.Provider>
                        </AssetProvider>
                    </LanguageProvider>
                </>
            )}
        </>
    )
}
