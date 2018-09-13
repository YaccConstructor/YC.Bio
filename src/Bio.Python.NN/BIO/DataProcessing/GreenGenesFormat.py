"""
GreenGenes database to appropriate format. Select only bacteria with full classification
"""

f1 = "DataProcessing/Data/gg_16S_unaligned.fasta"
f2 = "DataProcessing/Data/gg_16s_format.fasta"
with open(f1, 'r') as inp, open(f2, 'w') as out:
    data = inp.read().split('\n>')
    for d in data:
        if all(x in d for x in ["k__Bacteria", "p_", "c_", "o_", "f_", "g_", "s_"]):
            meta = map(lambda x: x.strip()[3:], d.split("k__Bacteria;")[1].split(';')[0:6])
            d2 = ">" + d.split(' ')[1] + " " + "Bacteria;" + ";".join(meta) + "\n" + "\n".join(d.split("\n")[1:]) + "\n"
            out.write(d2)