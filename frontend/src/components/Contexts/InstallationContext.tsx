import { createContext, useState, useEffect } from 'react'
import { Installation } from 'models/Installation'
import { useBackendApi } from 'api/UseBackendApi'
import { Outlet, useNavigate, useParams } from 'react-router-dom'
import { CircularProgress } from '@equinor/eds-core-react'
import { Typography } from '@equinor/eds-core-react'
import { useAssetContext } from './AssetContext'

interface IInstallationContext {
    installation: Installation
}

export const InstallationContext = createContext<IInstallationContext>(undefined!)

const useInstallationOrUndefined = (installationCode: string): Installation | undefined => {
    const [installation, setInstallation] = useState<Installation | undefined>(undefined)
    const backendApi = useBackendApi()
    const navigate = useNavigate()

    useEffect(() => {
        backendApi
            .getInstallations()
            .then((installations) => {
                const foundInstallation = installations.find(
                    (i) => i.installationCode.toLowerCase() === installationCode.toLowerCase()
                )
                if (!foundInstallation) {
                    console.error(`Installation with code ${installationCode} not found`)
                    navigate('/not-found')
                }
                setInstallation(foundInstallation)
            })
            .catch(() => {
                console.error(`Failed to retrieve installations for installation code ${installationCode}`)
            })
    }, [installationCode, backendApi])

    return installation
}

export const InstallationLayout = () => {
    const { installationCode } = useParams()
    const installation = useInstallationOrUndefined(installationCode!)
    const { switchInstallation } = useAssetContext()

    useEffect(() => {
        // TODO: This should be removed, but it is kept until useAssetContext uses the installation context
        if (installation) {
            switchInstallation(installation.id)
        }
    }, [installation])

    if (!installation) {
        return (
            <>
                <CircularProgress />
                <Typography variant="h2">Loading installation data...</Typography>
            </>
        )
    }

    return (
        <>
            <InstallationContext.Provider
                value={{
                    installation,
                }}
            >
                <Outlet />
            </InstallationContext.Provider>
        </>
    )
}
