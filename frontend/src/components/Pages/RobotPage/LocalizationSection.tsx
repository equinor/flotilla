import { Autocomplete, AutocompleteChanges, Button, Typography } from '@equinor/eds-core-react'
import { translateText } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { AssetDeck } from 'models/AssetDeck'
import { BackendAPICaller } from 'api/ApiCaller'

interface RobotProps {
    robot: Robot
}

export function LocalizationSection({ robot }: RobotProps) {
    const [selectedAssetDeck, setSelectedAssetDeck] = useState<AssetDeck>()
    const [assetDecks, setAssetDecks] = useState<AssetDeck[]>()

    useEffect(() => {
        BackendAPICaller.getAssetDecks().then((response: AssetDeck[]) => {
            setAssetDecks(response)
        })
    }, [])

    const getAssetDeckNames = (assetDecks: AssetDeck[]): Map<string, AssetDeck> => {
        var assetDeckNameMap = new Map<string, AssetDeck>()
        assetDecks.map((assetDeck: AssetDeck) => {
            assetDeckNameMap.set(assetDeck.deckName, assetDeck)
        })
        return assetDeckNameMap
    }
    const assetDeckNames = assetDecks !== undefined ? Array.from(getAssetDeckNames(assetDecks).keys()).sort() : []

    const onSelectedDeck = (changes: AutocompleteChanges<string>) => {
        const selectedDeckName = changes.selectedItems[0]
        const selectedAssetDeck = assetDecks?.find((assetDeck) => assetDeck.deckName === selectedDeckName)
        setSelectedAssetDeck(selectedAssetDeck)
    }

    const onClickLocalize = () => {
        if (selectedAssetDeck) {
            BackendAPICaller.postLocalizationMission(selectedAssetDeck?.defaultLocalizationPose, robot.id)
        }
    }
    return (
        <>
            <Typography variant="h2">{translateText('Localization')}</Typography>
            <Autocomplete
                options={assetDeckNames}
                label={translateText('Select deck')}
                onOptionsChange={onSelectedDeck}
            />
            <Button onClick={onClickLocalize}> {translateText('Localize')} </Button>
        </>
    )
}
