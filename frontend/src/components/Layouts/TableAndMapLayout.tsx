import { ReactNode } from 'react'
import styled from 'styled-components'
import { ContentCard } from 'components/Styles/StyledComponents'

const Layout = styled.div`
    display: grid;
    width: 100%;
    max-width: 100%;
    min-width: 0;
    gap: 2rem;
`

const PrimarySection = styled(ContentCard)`
    min-width: 0;
`

const TableAndMap = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: stretch;
    gap: 30px;
    min-width: 0;
    overflow-x: auto;
`

const FullWidthRows = styled.div`
    /* Scrolling rows must not contribute to the table/map's intrinsic width. */
    contain: inline-size;
    min-width: 0;
    display: grid;
    gap: 2rem;
    > * {
        min-width: 0;
    }
`

export const TableAndMapLayout = ({
    title,
    table,
    map,
    children,
}: {
    title: ReactNode
    table: ReactNode
    map?: ReactNode
    children?: ReactNode
}) => (
    <Layout>
        <PrimarySection>
            {title}
            <TableAndMap>
                {table}
                {map}
            </TableAndMap>
        </PrimarySection>
        {children && <FullWidthRows>{children}</FullWidthRows>}
    </Layout>
)
