import { Autocomplete, AutocompleteChanges, Button, Typography } from '@equinor/eds-core-react'
import { Text } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { useEffect, useState } from 'react'
import { AssetDeck } from 'models/AssetDeck'
import { useApi } from 'api/ApiCaller'
import { useAssetContext } from 'components/Contexts/AssetContext'

interface RobotProps {
    robot: Robot
}

export function LocalizationSection({ robot }: RobotProps) {
    const { assetCode } = useAssetContext()
    const [selectedAssetDeck, setSelectedAssetDeck] = useState<AssetDeck>()
    const [assetDecks, setAssetDecks] = useState<AssetDeck[]>()
    const apiCaller = useApi()

    useEffect(() => {
        apiCaller.getAssetDecks().then((response: AssetDeck[]) => {
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
    const assetDeckNames = assetDecks !== undefined ? Array.from(getAssetDeckNames(assetDecks).keys()) : []

    const onSelectedDeck = (changes: AutocompleteChanges<string>) => {
        const selectedDeckName = changes.selectedItems[0]
        const selectedAssetDeck = assetDecks?.find((assetDeck) => assetDeck.deckName === selectedDeckName)
        setSelectedAssetDeck(selectedAssetDeck)
    }

    const onClickLocalize = () => {
        if (selectedAssetDeck) {
            apiCaller.postLocalizationMission(selectedAssetDeck?.defaultLocalizationPose, robot.id)
        }
    }
    return (
        <>
            <Typography variant="h2">{Text('Localization')}</Typography>
            <Autocomplete options={assetDeckNames} label={Text('Select deck')} onOptionsChange={onSelectedDeck} />
            <Button onClick={onClickLocalize}> {Text('Localize')} </Button>
        </>
    )
}
