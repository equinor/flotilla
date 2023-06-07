import { Table, Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { AssetDeck } from 'models/AssetDeck'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { RefreshProps } from './AssetDecksPage'
import { tokens } from '@equinor/eds-tokens'

const StyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
    :hover {
        background-color: #deedee;
    }
`

const StyledDeckCards = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`

const TableWithHeader = styled.div`
    gap: 2rem;
`
const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4rem;
`

export function AssetDecksDialog({ refreshInterval }: RefreshProps) {
    const [assetDecks, setAssetDecks] = useState<AssetDeck[]>()
    const { assetCode } = useAssetContext()

    useEffect(() => {
        BackendAPICaller.getAssetDecks().then((response: AssetDeck[]) => {
            setAssetDecks(response)
        })
    }, [])

    const findSelectedDecks = (assetDecks: AssetDeck[]): AssetDeck[] => {
        const selectedAssetDecks = assetDecks?.filter(
            (assetDeck) => assetDeck.assetCode.toLowerCase() === assetCode.toLowerCase()
        )
        return selectedAssetDecks
    }

    const assetDecksList = assetDecks ? Array.from(findSelectedDecks(assetDecks)) : []

    return (
        <>
            <StyledContent>
                <StyledDeckCards>
                    {assetDecksList.map((deck) => (
                        <StyledCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
                            <Typography>{deck.deckName.toLocaleUpperCase()}</Typography>
                        </StyledCard>
                    ))}
                </StyledDeckCards>
                <TableWithHeader>
                    <Typography variant="h1">{TranslateText('Deck Inspections')}</Typography>
                    <Table>
                        <Table>
                            <Table.Head sticky>
                                <Table.Row>
                                    <Table.Cell>{TranslateText('Status')}</Table.Cell>
                                    <Table.Cell>{TranslateText('Name')}</Table.Cell>
                                    <Table.Cell>{TranslateText('Robot')}</Table.Cell>
                                </Table.Row>
                            </Table.Head>
                        </Table>
                    </Table>
                </TableWithHeader>
            </StyledContent>
        </>
    )
}
