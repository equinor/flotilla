import { createContext, FC, useContext, useState } from 'react'

interface IAssetContext {
    asset: string
    switchAsset: (newAsset: string) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAsset = {
    asset: 'test',
    switchAsset: (newAsset: string) => {},
}

export const assetOptions = new Map<string, string>([
    ['Test', 'test'],
    ['Kårstø', 'kaa'],
    ['Johan Sverdrup', 'js'],
])

export const AssetContext = createContext<IAssetContext>(defaultAsset)

export const AssetProvider: FC<Props> = ({ children }) => {
    const [asset, setAsset] = useState(defaultAsset.asset)

    const switchAsset = (newAsset: string) => {
        assetOptions.has(newAsset)
            ? setAsset(assetOptions.get(newAsset)!)
            : console.log('Could not find asset: ', newAsset)

        sessionStorage.setItem('assetString', newAsset)
        console.log('Saved asset: ', sessionStorage.getItem('assetString'))
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
