"""Editor-required identities for locally authored nested-prefab definitions."""
import uuid
import xml.etree.ElementTree as ET

NAMESPACE = uuid.UUID('b5e20c1a-8177-4d38-ad42-0a78fd08b68f')
ZERO = '{00000000-0000-0000-0000-000000000000}'

def ensure_definition_links(root, key):
    added = 0
    for index, node in enumerate(list(root.iter())):
        if node.tag not in ('entity', 'script') and not node.tag.endswith('_component'):
            continue
        if node.find('prefab_link') is not None:
            continue
        link = ET.Element('prefab_link')
        guid = str(uuid.uuid5(NAMESPACE, f'{key}/definition/{index}/{node.tag}/{node.get("name", "")}')).upper()
        ET.SubElement(link, 'instance', guid='{' + guid + '}')
        ET.SubElement(link, 'prefab', guid=ZERO)
        node.insert(0, link)
        added += 1
    return added
