import { BackendAPICaller } from 'api/ApiCaller'
import { useContext } from 'react'
import { InstallationContext } from './PlantContext'
import { AuthContext } from './AuthProvider'

type Props = {
    children?: React.ReactNode
}

// Can't use contexts inside the static class so we need a component to update it
export const APIUpdater = (props: Props) => {
    const accessToken = useContext(AuthContext)
    const { currentPlant } = useContext(InstallationContext)
    const installationCode = currentPlant ? currentPlant.installationCode : ''
    BackendAPICaller.accessToken = accessToken
    BackendAPICaller.installationCode = installationCode
    return <>{props.children}</>
}
