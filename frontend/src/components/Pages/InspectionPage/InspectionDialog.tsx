import { Table, Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Area } from 'models/Area'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { RefreshProps } from './InspectionPage'
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

export function AreasDialog({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const [Areas, setAreas] = useState<Area[]>()
    const { installationCode } = useInstallationContext()

    useEffect(() => {
        BackendAPICaller.getAreas().then((response: Area[]) => {
            setAreas(response)
        })
    }, [])

    const findSelectedDecks = (Areas: Area[]): Area[] => {
        const selectedAreas = Areas?.filter(
            (Area) => Area.installationCode.toLowerCase() === installationCode.toLowerCase()
        )
        return selectedAreas
    }

    const AreasList = Areas ? Array.from(findSelectedDecks(Areas)) : []

    return (
        <>
            <StyledContent>
                <StyledDeckCards>
                    {AreasList.map((deck) => (
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
