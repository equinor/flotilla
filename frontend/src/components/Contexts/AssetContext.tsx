import { createContext, FC, useContext, useState } from 'react'

interface IAssetContext {
    asset: string
    switchAsset: (newAsset: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAsset = {
    asset: '',
    switchAsset: (newAsset: string) => { },
}

export const AssetContext = createContext<IAssetContext>(defaultAsset)

export const AssetProvider: FC<Props> = ({ children }) => {
    const prevAsset = window.localStorage.getItem('assetString')
    const [asset, setAsset] = useState(prevAsset || defaultAsset.asset)

    const switchAsset = (newAsset: string) => {
        setAsset(newAsset)
        window.localStorage.setItem('assetString', newAsset)
    }

    return (
        <AssetContext.Provider
            value={{
                asset,
                switchAsset,
            }}
        >
            {children}
        </AssetContext.Provider>
    )
}

export const useAssetContext = () => useContext(AssetContext)
