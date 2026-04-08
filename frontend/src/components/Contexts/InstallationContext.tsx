import { createContext, useState, useEffect } from 'react'
import { Installation } from 'models/Installation'
import { useBackendApi } from 'api/UseBackendApi'
import { useNavigate } from 'react-router-dom'

interface IInstallationContext {
    installation: Installation
}

export const InstallationContext = createContext<IInstallationContext>(undefined!)

export const useInstallationOrUndefined = (installationCode: string): Installation | undefined => {
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
