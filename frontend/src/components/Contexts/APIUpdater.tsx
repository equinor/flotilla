import { BackendAPICaller } from 'api/ApiCaller'
import { useContext } from 'react'
import { RobotContext } from './RobotContext'
import { AuthContext } from './AuthProvider'

type Props = {
    children?: React.ReactNode
}

// Can't use contexts inside the static class so we need a component to update it
export const APIUpdater = (props: Props) => {
    const accessToken = useContext(AuthContext)
    const installationCode = useContext(RobotContext).installationCode
    BackendAPICaller.accessToken = accessToken
    BackendAPICaller.installationCode = installationCode
    return <>{props.children}</>
}
