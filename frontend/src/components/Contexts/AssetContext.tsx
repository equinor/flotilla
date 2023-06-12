import { createContext, FC, useContext, useState } from 'react'

interface IAssetContext {
    assetCode: string
    switchAsset: (selectedAsset: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAsset = {
    assetCode: '',
    switchAsset: (selectedAsset: string) => {},
}

export const AssetContext = createContext<IAssetContext>(defaultAsset)

export const AssetProvider: FC<Props> = ({ children }) => {
    const previousAsset = window.localStorage.getItem('assetString')
    const [assetCode, setAsset] = useState(previousAsset || defaultAsset.assetCode)

    const switchAsset = (selectedAsset: string) => {
        setAsset(selectedAsset.toLowerCase())
        window.localStorage.setItem('assetString', selectedAsset.toLowerCase())
    }

    return (
        <AssetContext.Provider
            value={{
                assetCode,
                switchAsset,
            }}
        >
            {children}
        </AssetContext.Provider>
    )
}

export const useAssetContext = () => useContext(AssetContext)
