import { createContext, FC, useContext, useState } from "react";

interface IAssetContext {
    asset: string,
    switchAsset: (newAsset: string) => void;
}

interface Props {
    children: React.ReactNode;
}

const defaultAsset = {
    asset: 'Test',
    switchAsset: (newAsset: string) => { }
}



export const AssetContext = createContext<IAssetContext>(defaultAsset);

export const AssetProvider: FC<Props> = ({ children }) => {
    const [asset, setAsset] = useState(defaultAsset.asset);

    const switchAsset = (newAsset: string) => {
        setAsset(newAsset);
        sessionStorage.setItem('asset', newAsset)
        console.log('Saved asset: ', sessionStorage.getItem('asset'))
    };

    return (
        <AssetContext.Provider
            value={{
                asset,
                switchAsset,
            }}
        >
            {children}
        </AssetContext.Provider>
    );
};

export const useAssetContext = () => useContext(AssetContext);