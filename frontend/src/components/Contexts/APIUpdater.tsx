import { BackendAPICaller } from 'api/ApiCaller'
import { useContext } from 'react'
import { AssetContext } from './AssetContext'
import { AuthContext } from './AuthProvider'

type Props = {
    children?: React.ReactNode
}

// Can't use contexts inside the static class so we need a component to update it
export const APIUpdater = (props: Props) => {
    const accessToken = useContext(AuthContext)
    const assetCode = useContext(AssetContext).assetCode
    BackendAPICaller.accessToken = accessToken
    BackendAPICaller.assetCode = assetCode
    return <>{props.children}</>
}
